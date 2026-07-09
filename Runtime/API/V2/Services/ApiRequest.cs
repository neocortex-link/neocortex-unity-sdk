using System;
using System.Linq;
using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using UnityEngine.Networking;
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
        private static string BaseURL => string.IsNullOrEmpty(BaseUrlOverride) ? "https://api.neocortex.link/v2" : BaseUrlOverride;
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
        private readonly JsonSerializerSettings jsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
        };

        public event Action<string> OnTranscriptionReceived;
        public event Action<AudioClip> OnAudioResponseReceived;
        public event Action<ChatResponse> OnChatResponseReceived;
        public event Action<string> OnRequestFailed;
        public event Action<Message[]> OnChatHistoryReceived;

        private string message;
        private string emotion;

        private void SetHeaders()
        {
            if (settings == null || string.IsNullOrEmpty(settings.apiKey))
            {
                throw new Exception("API Key is required. Please add it in the Tools > Neocortex > API Key Setup.");
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
                    List<IMultipartFormSection> form = new List<IMultipartFormSection>
                    {
                        new MultipartFormFileSection("audio", (input as AudioClip).EncodeToWav(), "audio.wav", "audio/wav"),
                        new MultipartFormDataSection("characterId", characterId)
                    };

                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BaseURL}/audio/transcribe",
                        data = form,
                        responseType = ApiResponseType.Text
                    };

                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, jsonSerializerSettings);

                    message = response.response;
                    OnTranscriptionReceived?.Invoke(message);
                }
                else
                {
                    message = input as string;
                }

                // here chat request
                {
                    var data = new
                    {
                        sessionId = NeocortexSessionManager.GetSessionID(characterId),
                        playerId = SystemInfo.deviceUniqueIdentifier,
                        characterId,
                        message,
                        metadata = CreateMetadata(),
                        events = NeocortexEventLogger.GetLogs(),
                    };

                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BaseURL}/chat",
                        data = GetBytes(data),
                        responseType = ApiResponseType.Text
                    };

                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, jsonSerializerSettings);

                    NeocortexSessionManager.SetSessionID(characterId, response.sessionId);

                    message = response.response;
                    emotion = response.emotion.ToString().ToUpper();
                    OnChatResponseReceived?.Invoke(new ChatResponse()
                    {
                        message = message,
                        emotion = response.emotion,
                        action = response.action,
                        flowState =  response.flowState,
                        metadata = response.metadata
                    });
                }

                // here audio request
                if (typeof(TOutput) == typeof(AudioClip))
                {
                    var data = new
                    {
                        characterId,
                        message,
                        emotion,
                    };

                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BaseURL}/audio/generate",
                        data = GetBytes(data),
                        responseType = ApiResponseType.Audio
                    };

                    UnityWebRequest request = await Send(payload);
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);

                    OnAudioResponseReceived?.Invoke(audioClip);
                }
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
            }
        }

        private string CreateMetadata()
        {
            NeocortexInteractable[] interactables = Object.FindObjectsByType<NeocortexInteractable>(FindObjectsSortMode.None);
            string metadata = "";
            if (interactables.Length > 0)
            {
                var interactableList = interactables.Select(i => i.ToInteractable()).ToList();
                metadata = JsonConvert.SerializeObject(interactableList);
            }
            
            return metadata;
        }

        public async void GetChatHistory(string characterID, int limit = 10)
        {
            try
            {
                SetHeaders();

                var data = new
                {
                    limit,
                    sessionId = NeocortexSessionManager.GetSessionID(characterID),
                };

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BaseURL}/chat/session",
                    data = GetBytes(data),
                    responseType = ApiResponseType.Text
                };

                UnityWebRequest request = await Send(payload);
                ApiChatHistory response = JsonConvert.DeserializeObject<ApiChatHistory>(request.downloadHandler.text, jsonSerializerSettings);

                OnChatHistoryReceived?.Invoke(response.messages);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
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
                    method = UnityWebRequest.kHttpVerbGET,
                    responseType = ApiResponseType.Text
                };

                UnityWebRequest request = await Send(payload);
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
                    method = UnityWebRequest.kHttpVerbGET,
                    responseType = ApiResponseType.Text
                };

                UnityWebRequest request = await Send(payload);
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

        private string GetRequestError()
        {
            try
            {
                ApiErrorResponse error = JsonConvert.DeserializeObject<ApiErrorResponse>(LastError);
                if (!string.IsNullOrEmpty(error?.error))
                {
                    return error.error;
                }
            }
            catch
            {
                // Body was not the { "error": ... } shape; fall back to the raw text below.
            }

            return string.IsNullOrEmpty(LastError) ? $"Request failed ({LastResponseCode})" : LastError;
        }
    }
}
