using System;
using System.Linq;
using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Object = UnityEngine.Object;

namespace Neocortex.API
{
    public class ApiRequest : WebRequest
    {
        public static string BaseUrlOverride;

        // Cleared for good the first time a server answers 404 for streamed speech.
        private static bool streamedSpeechSupported = true;

        private static string BaseURL => string.IsNullOrEmpty(BaseUrlOverride) ? "https://api.neocortex.link/v3" : BaseUrlOverride;

        // Ceilings, not expectations: a turn that takes this long has already failed the player,
        // and without one a request that never answers leaves an agent busy for the session.
        private const int TurnTimeoutSeconds = 30;
        private const int AudioTimeoutSeconds = 20;
        private const int LookupTimeoutSeconds = 15;
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
        private readonly JsonSerializerSettings jsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new SafeStringEnumConverter() }
        };

        public event Action<string> OnTranscriptionReceived;
        public event Action<AudioClip> OnAudioResponseReceived;
        public event Action<ChatResponse> OnChatResponseReceived;
        public event Action<string> OnRequestFailed;
        public event Action<ChatHistoryEntry[]> OnChatHistoryReceived;


        private string message;
        private string spokenMessage;
        private string emotion;

        private void SetHeaders()
        {
            if (settings == null || string.IsNullOrEmpty(settings.apiKey))
            {
                throw new Exception("API Key is required. Please add it in the Tools > Neocortex > Settings.");
            }

            Headers = new Dictionary<string, string>()
            {
                { "x-api-key", settings.apiKey }
            };
        }

        public async void Send<TInput, TOutput>(string characterId, TInput input)
        {
            try
            {
                if (string.IsNullOrEmpty(characterId))
                {
                    throw new Exception("Project ID is required");
                }

                SetHeaders();

                // here transcription request
                if (typeof(TInput) == typeof(AudioClip))
                {
                    message = await RequestTranscription(characterId, input as AudioClip);
                    OnTranscriptionReceived?.Invoke(message);
                }
                else
                {
                    message = input as string;
                }

                // here chat request (v3 unified endpoint: a single character is just a cast of one)
                {
                    var data = new
                    {
                        sessionId = NeocortexSessionManager.GetSessionID(characterId),
                        playerId = SystemInfo.deviceUniqueIdentifier,
                        characterIds = new[] { characterId },
                        message,
                        metadata = CreateMetadata(characterId),
                        events = NeocortexEventLogger.GetLogs()
                    };

                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BaseURL}/chat",
                        timeoutSeconds = TurnTimeoutSeconds,
                        data = GetBytes(data),
                        responseType = ApiResponseType.Text
                    };

                    using UnityWebRequest request = await Send(payload);

                    if (request == null)
                    {
                        throw new Exception(GetRequestError());
                    }

                    ApiChatResponse response = JsonConvert.DeserializeObject<ApiChatResponse>(request.downloadHandler.text, jsonSerializerSettings);

                    NeocortexSessionManager.SetSessionID(characterId, response.sessionId);

                    // Solo cast → exactly one speaker. Flatten it onto the ChatResponse the
                    // single-character agent has always emitted (message/emotion derived from the
                    // lines, since v3 sends only lines + stacked actions).
                    GroupMessage speaker = response.messages != null && response.messages.Length > 0 ? response.messages[0] : null;
                    ChatResponse chatResponse = ToChatResponse(speaker, response.metadata);

                    message = chatResponse.message;
                    spokenMessage = chatResponse.spokenMessage;
                    emotion = chatResponse.emotion.ToString().ToUpper();
                    OnChatResponseReceived?.Invoke(chatResponse);
                }

                // here audio request
                if (typeof(TOutput) == typeof(AudioClip))
                {
                    string speechText = !string.IsNullOrEmpty(spokenMessage) ? spokenMessage : message;
                    AudioClip audioClip = await GenerateAudio(characterId, speechText, emotion, spokenMessage);

                    if (audioClip != null)
                    {
                        OnAudioResponseReceived?.Invoke(audioClip);
                    }
                }
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        ///     Where perception is measured from, the speaking character's transform. Nearest
        ///     entities win when the scene has more than the character can take in. Null = no
        ///     distance filtering (the whole scene, still capped).
        /// </summary>
        public Transform PerceptionOrigin { get; set; }

        /// <summary>Only perceive entities within this many metres of the origin. 0 = unlimited.</summary>
        public float PerceptionRadius { get; set; }

        /// <summary>Hard ceiling on how many entities a single turn may carry.</summary>
        public int MaxPerceivedEntities { get; set; } = 24;

        /// <summary>
        ///     Builds what the character can perceive: the character's OWN location always goes in
        ///     (so it knows where it stands), plus every tagged entity in the scene, nearest first
        ///     and bounded so a busy scene can't blow up the prompt. Positions go out as raw facts,
        ///     the character works out distance, direction and relevance for itself.
        /// </summary>
        private string CreateMetadata(string selfCharacterId = null)
        {
            var entities = new List<Interactable>();

            NeocortexInteractable[] interactables = Object.FindObjectsByType<NeocortexInteractable>(FindObjectsSortMode.None);
            IEnumerable<NeocortexInteractable> perceived = interactables;

            if (PerceptionOrigin != null)
            {
                Vector3 origin = PerceptionOrigin.position;

                if (PerceptionRadius > 0)
                {
                    perceived = perceived.Where(i => Vector3.Distance(origin, i.transform.position) <= PerceptionRadius);
                }

                // Nearest first, so the cap keeps what matters most.
                perceived = perceived.OrderBy(i => Vector3.SqrMagnitude(i.transform.position - origin));
            }

            entities.AddRange(perceived.Take(Mathf.Max(1, MaxPerceivedEntities)).Select(i => i.ToInteractable()));

            // Always include the speaking character's own location so it knows where it is, unless
            // a NeocortexInteractable already represents it (auto-linked on its GameObject).
            if (!string.IsNullOrEmpty(selfCharacterId) && PerceptionOrigin != null &&
                !entities.Exists(e => e.characterId == selfCharacterId))
            {
                entities.Insert(0, new Interactable
                {
                    id = selfCharacterId,
                    characterId = selfCharacterId,
                    name = PerceptionOrigin.name,
                    position = PerceptionOrigin.position,
                    properties = new[]
                    {
                        new InteractableProperty { name = "name", value = PerceptionOrigin.name },
                        new InteractableProperty { name = "kind", value = "character" },
                    },
                    type = "CHARACTER",
                    isSubject = false,
                });
            }

            string metadata = entities.Count > 0 ? JsonConvert.SerializeObject(entities) : "";
            return metadata;
        }

        /// <summary>
        ///     Flattens a v3 speaker message onto the flat <see cref="ChatResponse"/> the
        ///     single-character agent emits: <c>message</c> is the lines joined, <c>emotion</c> the
        ///     first line's emotion, <c>action</c> the first stacked action. Scene metadata is passed
        ///     through from the turn's top level (it is scene-wide, not per speaker).
        /// </summary>
        public static ChatResponse ToChatResponse(GroupMessage message, Interactable[] sceneMetadata)
        {
            ChatLine[] lines = message?.lines ?? Array.Empty<ChatLine>();
            ChatAction[] actions = message?.actions ?? Array.Empty<ChatAction>();
            string derivedSpoken = lines.Any(l => !string.IsNullOrEmpty(l.spokenText))
                ? string.Concat(lines.Select(l => !string.IsNullOrEmpty(l.spokenText) ? l.spokenText : l.text))
                : message?.spokenMessage;

            return new ChatResponse
            {
                characterId = message?.characterId,
                name = message?.name,
                lines = lines,
                actions = actions,
                message = string.Concat(lines.Select(l => l.text)),
                spokenMessage = !string.IsNullOrEmpty(derivedSpoken) ? derivedSpoken : string.Concat(lines.Select(l => l.text)),
                emotion = lines.Length > 0 ? lines[0].emotion : Emotions.Neutral,
                action = actions.Length > 0 ? actions[0].name : string.Empty,
                flowState = message?.flowState,
                metadata = sceneMetadata
            };
        }

        /// <summary>
        ///     Sends one turn to the unified v3 chat endpoint for a whole cast and returns every
        ///     speaker's reply, in order. Pass 1..n character ids. Leave
        ///     <paramref name="speakerCharacterId"/> null to let the AI director choose who speaks and
        ///     in what order; set it to force a specific character to speak this turn. Omit
        ///     <paramref name="message"/> for an ambient turn (the cast talks among themselves).
        ///     Scene metadata and the event log are gathered automatically when not supplied.
        ///     Multi-character casts require a Pro/Team API key. Returns null and raises
        ///     <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        public async Task<GroupChatResponse> RequestGroupChat(
            string[] characterIds,
            string sessionId,
            string message = null,
            string speakerCharacterId = null,
            int? maxTurns = null,
            string metadata = null,
            string events = null)
        {
            try
            {
                SetHeaders();

                metadata ??= CreateMetadata();
                events ??= NeocortexEventLogger.GetLogs();

                var data = new Dictionary<string, object>
                {
                    ["characterIds"] = characterIds,
                    ["playerId"] = SystemInfo.deviceUniqueIdentifier
                };
                if (!string.IsNullOrEmpty(sessionId)) data["sessionId"] = sessionId;
                if (!string.IsNullOrEmpty(message)) data["message"] = message;
                if (!string.IsNullOrEmpty(speakerCharacterId)) data["speakerCharacterId"] = speakerCharacterId;
                if (maxTurns.HasValue) data["maxTurns"] = maxTurns.Value;
                if (!string.IsNullOrEmpty(metadata)) data["metadata"] = metadata;
                if (!string.IsNullOrEmpty(events)) data["events"] = events;

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/chat",
                    timeoutSeconds = TurnTimeoutSeconds,
                    data = GetBytes(data),
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);
                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return JsonConvert.DeserializeObject<GroupChatResponse>(request.downloadHandler.text, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        // Fire-and-forget history load for a single character's session, surfaced via
        // OnChatHistoryReceived. For pagination or the shared group session, use RequestChatHistory.
        public async void GetChatHistory(string characterID, int limit = 10)
        {
            string sessionId = NeocortexSessionManager.GetSessionID(characterID);
            ApiChatHistory response = await RequestChatHistory(sessionId, limit);
            if (response != null)
            {
                OnChatHistoryReceived?.Invoke(response.messages);
            }
        }

        /// <summary>
        ///     Fetches a session's transcript (oldest turn first). Keyed by session id, so it serves
        ///     both a single character's chat and the shared session of a
        ///     <see cref="NeocortexGroupDirector"/>. Pass the previous result's
        ///     <see cref="ApiChatHistory.nextCursor"/> as <paramref name="before"/> to page back to
        ///     older messages; the cursor is null once the start is reached. Returns null and raises
        ///     <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        public async Task<ApiChatHistory> RequestChatHistory(string sessionId, int limit = 10, string before = null)
        {
            try
            {
                SetHeaders();

                var data = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["limit"] = limit,
                };
                if (!string.IsNullOrEmpty(before)) data["before"] = before;

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/chat/session",
                    timeoutSeconds = LookupTimeoutSeconds,
                    data = GetBytes(data),
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);
                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return JsonConvert.DeserializeObject<ApiChatHistory>(request.downloadHandler.text, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        ///     Transcribes speech to text via the audio endpoint, WITHOUT chatting (unlike
        ///     <see cref="Send{TInput,TOutput}"/>). The character id is required for auth/metering,
        ///     any character the key owns works. Returns "" and raises
        ///     <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        public async Task<string> RequestTranscription(string characterId, AudioClip clip)
        {
            try
            {
                SetHeaders();

                List<IMultipartFormSection> form = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection("audio", clip.EncodeToWav(), "audio.wav", "audio/wav"),
                    new MultipartFormDataSection("characterId", characterId)
                };

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/audio/transcribe",
                    timeoutSeconds = AudioTimeoutSeconds,
                    data = form,
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);

                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, jsonSerializerSettings);
                return response.response;
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return "";
            }
        }

        /// <summary>
        ///     Generates speech for a piece of text, voiced in the given emotion.
        ///     IMPORTANT: every call costs 1 audio credit, generating audio per line multiplies
        ///     the audio cost of a reply by the number of lines.
        ///     Returns null and raises <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        public async Task<AudioClip> GenerateAudio(string characterId, string message, string emotion, string spokenText = null)
        {
            try
            {
                SetHeaders();

                string speechText = !string.IsNullOrEmpty(spokenText) ? spokenText : message;

                var data = new
                {
                    characterId,
                    message = speechText,
                    spokenText = speechText,
                    emotion,
                };

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/audio/generate",
                    timeoutSeconds = AudioTimeoutSeconds,
                    data = GetBytes(data),
                    responseType = ApiResponseType.Audio
                };

                using UnityWebRequest request = await Send(payload);

                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return DownloadHandlerAudioClip.GetContent(request);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        ///     Gets the developer account info (tier, owner email, remaining credits, next refresh).
        ///     Read-only and unmetered. Returns null and raises <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        public async Task<ApiAccountResponse> GetAccount()
        {
            try
            {
                SetHeaders();

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/account",
                    timeoutSeconds = LookupTimeoutSeconds,
                    method = UnityWebRequest.kHttpVerbGET,
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);
                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return JsonConvert.DeserializeObject<ApiAccountResponse>(request.downloadHandler.text, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        ///     Gets the API key's team's characters (id + name). Read-only and unmetered, powers
        ///     the editor's character picker. Returns null and raises <see cref="OnRequestFailed"/>
        ///     on failure.
        /// </summary>
        public async Task<ApiCharactersResponse> GetCharacters()
        {
            try
            {
                SetHeaders();

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/characters",
                    timeoutSeconds = LookupTimeoutSeconds,
                    method = UnityWebRequest.kHttpVerbGET,
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);
                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return JsonConvert.DeserializeObject<ApiCharactersResponse>(request.downloadHandler.text, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        ///     Gets team credit status plus optional per-player / per-character usage.
        ///     Read-only and unmetered. An unknown playerId returns zero usage, not an error.
        ///     Returns null and raises <see cref="OnRequestFailed"/> on failure.
        /// </summary>
        /// <param name="playerId">The external player id the game already uses for chat/sessions.</param>
        /// <param name="characterId">The character (project) id to get usage for.</param>
        public async Task<ApiUsageResponse> GetUsage(string playerId = null, string characterId = null)
        {
            try
            {
                SetHeaders();

                List<string> query = new List<string>();
                if (!string.IsNullOrEmpty(playerId))
                {
                    query.Add($"playerId={UnityWebRequest.EscapeURL(playerId)}");
                }
                if (!string.IsNullOrEmpty(characterId))
                {
                    query.Add($"characterId={UnityWebRequest.EscapeURL(characterId)}");
                }

                string queryString = query.Count > 0 ? $"?{string.Join("&", query)}" : "";

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/usage{queryString}",
                    timeoutSeconds = LookupTimeoutSeconds,
                    method = UnityWebRequest.kHttpVerbGET,
                    responseType = ApiResponseType.Text
                };

                using UnityWebRequest request = await Send(payload);
                if (request == null)
                {
                    throw new Exception(GetRequestError());
                }

                return JsonConvert.DeserializeObject<ApiUsageResponse>(request.downloadHandler.text, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        ///     EXPERIMENTAL. Generates speech and delivers it as it is synthesized, so playback can
        ///     start before the whole line is ready.
        ///     Returns false and raises <see cref="OnRequestFailed"/> when nothing could be played.
        /// </summary>
        /// <param name="characterId">The character whose voice is used.</param>
        /// <param name="message">The line to speak.</param>
        /// <param name="emotion">Emotion the line is voiced in.</param>
        /// <param name="onFormat">Raised once with sample rate and channel count.</param>
        /// <param name="onSamples">Raised repeatedly with the audio as it arrives.</param>
        /// <param name="cancellationToken">Cancels this request only.</param>
        /// <summary>
        ///     Speaks text by playing it as it is generated. Returns false when nothing could be
        ///     streamed, so the caller can fall back to a whole clip.
        /// </summary>
        /// <param name="reportErrors">
        ///     Whether a failure raises OnRequestFailed. A caller that falls back should leave this
        ///     off, so a recovered turn does not look like a broken one.
        /// </param>
        public async Task<bool> GenerateAudioStream(
            string characterId,
            string message,
            string emotion,
            Action<int, int> onFormat,
            Action<float[]> onSamples,
            CancellationToken cancellationToken = default,
            bool reportErrors = true)
        {
            // A server without the streaming endpoint will not grow one mid-session, so stop
            // paying for a doomed request before every single line.
            if (!streamedSpeechSupported) return false;

            try
            {
                SetHeaders();

                var data = new
                {
                    characterId,
                    message,
                    spokenText = message,
                    emotion,
                };

                // Wrapped so the moment sound becomes possible is recorded without the
                // caller having to time it, since that is the number this path exists for.
                PcmStreamDownloadHandler handler = new PcmStreamDownloadHandler(onFormat, onSamples);

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/audio/stream",
                    timeoutSeconds = AudioTimeoutSeconds,
                    data = GetBytes(data),
                    responseType = ApiResponseType.Stream,
                    downloadHandler = handler
                };

                using UnityWebRequest request = await Send(payload, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (request == null)
                {
                    if (LastResponseCode == 404) streamedSpeechSupported = false;
                    if (reportErrors) OnRequestFailed?.Invoke(GetRequestError());
                    return false;
                }

                if (handler.IsNotAudio)
                {
                    if (reportErrors)
                    {
                        OnRequestFailed?.Invoke(string.IsNullOrEmpty(handler.RawText) ? "Speech stream returned no audio." : handler.RawText);
                    }
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                if (reportErrors)
                {
                    OnRequestFailed?.Invoke(e.Message);
                    Debug.LogError(e.Message);
                }
                return false;
            }
        }


        private string GetRequestError()
        {
            try
            {
                ApiErrorResponse error = JsonConvert.DeserializeObject<ApiErrorResponse>(LastError, jsonSerializerSettings);
                if (!string.IsNullOrEmpty(error?.error))
                {
                    return error.error;
                }
            }
            catch
            {
                // Not the { "error": ... } shape, so whatever answered was not the API.
            }

            return DescribeFailure(LastError, LastResponseCode, null);
        }
    }
}
