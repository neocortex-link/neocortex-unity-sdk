using System;
using System.Linq;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Neocortex
{
    /// <summary>
    ///     A Neocortex character: sends messages, speaks the reply as chat lines, and runs the actions it triggers.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Smart Agent", 0)]
    public class NeocortexSmartAgent : MonoBehaviour
    {
        // How many line clips are generated at once in Per-Line Audio mode.
        private const int MAX_CONCURRENT_AUDIO = 2;

        [Tooltip("The character (project) id this agent talks to.")]
        public string characterID;

        [Tooltip("Off: always single message replies.\n" +
                 "Text: multiple chat lines drop in with separate emotions.\n" +
                 "Single Audio: same, plus ONE voice clip for the whole reply.\n" +
                 "Per-Line Audio: same, but each line is voiced separately for faster responses.")]
        public ChatLinesMode chatLinesMode = ChatLinesMode.PerLineAudio;

        [Tooltip("Where line clips play. Added automatically when you pick an audio mode; falls back to an AudioSource on this GameObject.")]
        public AudioSource audioSource;

        [Tooltip("Fetch and replay this character's stored conversation via OnChatHistoryReceived when the scene starts.")]
        public bool loadHistoryOnStart;

        [Tooltip("Log each chat line the character speaks, and any action it asks for, to the Console.")]
        public bool logToConsole = true;

        [Tooltip("Listen through a microphone on this GameObject and answer by itself. Turn off when a Chat UI, a group director, or your own script feeds the agent instead.")]
        public bool autoVoiceInput = true;

        [Tooltip("When Response Received: actions run the instant the reply arrives.\n" +
                 "When Speech Starts: actions run as the character begins speaking.\n" +
                 "After Reply Spoken: actions wait until the character has finished speaking.\n" +
                 "The last two need an audio Chat Lines Mode; Off mode falls back to firing on arrival.")]
        public ActionTrigger actionTrigger = ActionTrigger.WhenResponseReceived;

        [Space] public UnityEvent<ChatResponse> OnChatResponseReceived = new();
        [Space] public UnityEvent<AudioClip> OnAudioResponseReceived = new();
        [Space] public UnityEvent<string> OnTranscriptionReceived = new();
        [Space] public UnityEvent<string> OnRequestFailed = new();
        [Space] public UnityEvent<ChatHistoryEntry[]> OnChatHistoryReceived = new();

        [Header("Chat Lines")]
        [Tooltip("Raised as each chat line drops in, add it to your chat panel here.")]
        [Space] public UnityEvent<ChatLine> OnChatLineStarted = new();
        [Tooltip("Raised with each line's emotion as it drops in. Drive animation here.")]
        [Space] public UnityEvent<Emotions> OnEmotionChanged = new();
        [Tooltip("Text mode: raised during the pause BEFORE the next line, so a 'typing…' indicator can fill the gap.")]
        [Space] public UnityEvent OnComposingNextLine = new();
        [Tooltip("Raised once the whole reply has finished playing.")]
        [Space] public UnityEvent OnReplyFinished = new();

        [Header("Actions")]
        [Tooltip("Raised once a reply's actions have all been run.")]
        [Space] public UnityEvent OnActionsCompleted = new();

        private enum ReplyState { Idle, WaitingForReply, Playing }

        private ApiRequest apiRequest;
        private AudioReceiver voiceInput;
        private ReplyState state = ReplyState.Idle;
        private readonly Queue<Action> pendingInputs = new();
        private int playbackToken;
        private NeocortexUsageGate usageGate;

        private readonly Dictionary<string, Func<ChatAction, IEnumerator>> actionHandlers = new();
        private readonly Queue<ChatAction> actionQueue = new();
        private bool actionsRunning;
        private bool warnedOffMode;

        // The mode the editor last brought components along for, so they arrive once when you switch
        // into an audio mode and deleting them afterwards sticks.
        [SerializeField, HideInInspector] private ChatLinesMode lastEditorMode = ChatLinesMode.Off;

        /// <summary>True while a reply is pending or its chat lines are still being played back.</summary>
        public bool IsSpeaking => state != ReplyState.Idle;

        /// <summary>True while the reply's actions are being run.</summary>
        public bool IsRunningActions => actionsRunning;

        private void Awake()
        {
            // An AudioSource on the same GameObject counts as assigned; an audio mode set from code
            // gets one made for it, so playback never silently falls back to text.
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null && NeedsAudio(chatLinesMode))
            {
                audioSource = AddAudioSource();
            }

            apiRequest = new ApiRequest();
            // The character perceives the world from where it stands.
            apiRequest.PerceptionOrigin = transform;
            apiRequest.PerceptionRadius = 0;
            apiRequest.MaxPerceivedEntities = 10;
            apiRequest.OnChatResponseReceived += HandleChatResponse;
            apiRequest.OnAudioResponseReceived += OnAudioResponseReceived.Invoke;
            apiRequest.OnTranscriptionReceived += HandleTranscription;
            apiRequest.OnChatHistoryReceived += OnChatHistoryReceived.Invoke;
            apiRequest.OnRequestFailed += HandleRequestFailed;
        }

        private void Start()
        {
            // An empty session id would be rejected by the server, so only load when one exists.
            if (loadHistoryOnStart && NeocortexSessionManager.GetSessionID(characterID) != "")
            {
                GetChatHistory();
            }

            // Runs in Start, so anything that drives the agent itself has had its Awake to opt out.
            if (autoVoiceInput)
            {
                BindVoiceInput();
            }
        }

        private void OnDestroy()
        {
            playbackToken++; // cancel any in-flight chat-line playback
        }

        public void TextToText(string message)
        {
            Dispatch(() => apiRequest.Send<string, string>(characterID, message));
        }

        public void TextToAudio(string message)
        {
            // In a chat-lines mode the agent voices the reply itself, so request text only and
            // avoid paying for a separate full-reply clip.
            if (chatLinesMode == ChatLinesMode.Off)
            {
                Dispatch(() => apiRequest.Send<string, AudioClip>(characterID, message));
            }
            else
            {
                Dispatch(() => apiRequest.Send<string, string>(characterID, message));
            }
        }

        public void AudioToText(AudioClip audioClip)
        {
            Dispatch(() => apiRequest.Send<AudioClip, string>(characterID, audioClip));
        }

        public void AudioToAudio(AudioClip audioClip)
        {
            if (chatLinesMode == ChatLinesMode.Off)
            {
                Dispatch(() => apiRequest.Send<AudioClip, AudioClip>(characterID, audioClip));
            }
            else
            {
                Dispatch(() => apiRequest.Send<AudioClip, string>(characterID, audioClip));
            }
        }

        public void GetChatHistory(int limit = 10)
        {
            apiRequest.GetChatHistory(characterID, limit);
        }

        /// <summary>Fetches this character's history with pagination, without raising the event.</summary>
        /// <param name="limit">How many messages to fetch.</param>
        /// <param name="before">The previous result's nextCursor, to load older messages.</param>
        public Task<ApiChatHistory> RequestChatHistory(int limit = 10, string before = null)
        {
            return apiRequest.RequestChatHistory(NeocortexSessionManager.GetSessionID(characterID), limit, before);
        }

        /// <summary>Generates one audio clip for a chat line, voiced in its emotion. Costs 1 audio credit.</summary>
        /// <param name="line">The chat line to voice.</param>
        public Task<AudioClip> GenerateChatLineAudio(ChatLine line)
        {
            return apiRequest.GenerateAudio(characterID, line.text, line.emotion.ToString().ToUpper());
        }

        /// <summary>Assigns the coroutine that performs an action keyword, as authored on the character's Actions node.</summary>
        /// <param name="keyword">The action keyword, for example GO_TO_LOCATION.</param>
        /// <param name="handler">Coroutine to run; its ChatAction carries the targetId it applies to.</param>
        public void RegisterAction(string keyword, Func<ChatAction, IEnumerator> handler)
        {
            actionHandlers[keyword] = handler;
        }

        /// <summary>
        ///     Speaks a reply produced elsewhere, a <see cref="NeocortexGroupDirector"/> turn, through this
        ///     character. Awaitable, so the director can let one character finish before the next begins.
        /// </summary>
        /// <param name="message">The group turn this character should speak.</param>
        public async Task Speak(GroupMessage message)
        {
            ChatResponse response = ApiRequest.ToChatResponse(message, null);
            OnChatResponseReceived.Invoke(response);
            CaptureActions(response);

            if (chatLinesMode == ChatLinesMode.Off)
            {
                return;
            }

            state = ReplyState.Playing;
            int token = ++playbackToken;
            await PlayReply(ResolveLines(response), token);

            // A beat before the next character speaks.
            await WaitForSeconds(1f, playbackToken);
        }

        // Sends immediately in Off mode. In a chat-lines mode, queues the input while the character
        // is still speaking so nothing is submitted mid-reply.
        private void Dispatch(Action send)
        {
            if (chatLinesMode == ChatLinesMode.Off)
            {
                send();
                return;
            }

            if (state != ReplyState.Idle)
            {
                pendingInputs.Enqueue(send);
                return;
            }

            state = ReplyState.WaitingForReply;
            send();
        }

        private void HandleChatResponse(ChatResponse response)
        {
            OnChatResponseReceived.Invoke(response);
            CaptureActions(response);

            if (chatLinesMode == ChatLinesMode.Off)
            {
                return;
            }

            state = ReplyState.Playing;
            int token = ++playbackToken;
            _ = PlayReply(ResolveLines(response), token);
        }

        private void HandleTranscription(string transcription)
        {
            if (logToConsole)
            {
                Debug.Log($"[Neocortex] Player: {transcription}", this);
            }

            OnTranscriptionReceived.Invoke(transcription);
        }

        private void HandleRequestFailed(string error)
        {
            OnRequestFailed.Invoke(error);

            // A failed turn must not leave the mic closed, or the character goes deaf.
            RearmMicrophone();

            // Only a failure before playback should unblock the queue; per-line audio failures during
            // playback are handled inline (text still shows, advance).
            if (chatLinesMode != ChatLinesMode.Off && state == ReplyState.WaitingForReply)
            {
                state = ReplyState.Idle;
                playbackToken++;
                DispatchNext();
            }
        }

        // A reply with no lines (older server) plays as one line.
        private static ChatLine[] ResolveLines(ChatResponse response)
        {
            return response.lines != null && response.lines.Length > 0
                ? response.lines
                : new[] { new ChatLine { text = response.message, emotion = response.emotion } };
        }

        // ── Playback ────────────────────────────────────────────────────────────────────────────

        private async Task PlayReply(ChatLine[] lines, int token)
        {
            switch (chatLinesMode)
            {
                case ChatLinesMode.PerLineAudio:
                    await PlayPerLineAudio(lines, token);
                    break;
                case ChatLinesMode.SingleAudio:
                    await PlaySingleAudio(lines, token);
                    break;
                default:
                    await PlayTextOnly(lines, token);
                    break;
            }
        }

        private async Task PlayTextOnly(ChatLine[] lines, int token)
        {
            await DropLines(lines, token);
            if (this == null || token != playbackToken) return;
            FinishReply(token);
        }

        private async Task PlaySingleAudio(ChatLine[] lines, int token)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[Neocortex] Single Audio chat-lines mode needs an AudioSource. Showing text only (no credits spent).", this);
                await DropLines(lines, token);
                if (this == null || token != playbackToken) return;
                FinishReply(token);
                return;
            }

            // Don't spend a credit we don't have.
            if (await IsOutOfCredits(token))
            {
                if (this == null || token != playbackToken) return;
                await DropLines(lines, token);
                if (this == null || token != playbackToken) return;
                FinishReply(token);
                return;
            }

            await PlayOneClipAndDrop(JoinText(lines), lines, token);
            if (this == null || token != playbackToken) return;
            FinishReply(token);
        }

        private async Task PlayPerLineAudio(ChatLine[] lines, int token)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[Neocortex] Per-Line Audio chat-lines mode needs an AudioSource. Showing text only (no credits spent).", this);
                await DropLines(lines, token);
                if (this == null || token != playbackToken) return;
                FinishReply(token);
                return;
            }

            // Credit-aware degrade: empty → text only, low → one clip.
            usageGate ??= new NeocortexUsageGate();
            ApiUsageResponse usage = await usageGate.GetUsageCached(characterId: characterID);
            if (this == null || token != playbackToken) return;

            if (usage != null && usage.status == UsageStatus.Empty)
            {
                await DropLines(lines, token);
                if (this == null || token != playbackToken) return;
                FinishReply(token);
                return;
            }

            if (usage != null && usage.status == UsageStatus.Low)
            {
                await PlayOneClipAndDrop(JoinText(lines), lines, token);
                if (this == null || token != playbackToken) return;
                FinishReply(token);
                return;
            }

            // Fetch clips with a small concurrency cap; play strictly in line order, starting line 1
            // as soon as its clip returns while later lines keep synthesizing.
            Task<AudioClip>[] clipTasks = new Task<AudioClip>[lines.Length];
            int nextToStart = 0;

            void StartNext()
            {
                if (nextToStart < lines.Length)
                {
                    int index = nextToStart++;
                    clipTasks[index] = FetchClip(index);
                }
            }

            async Task<AudioClip> FetchClip(int index)
            {
                AudioClip c = await GenerateChatLineAudio(lines[index]);
                StartNext();
                return c;
            }

            for (int i = 0; i < MAX_CONCURRENT_AUDIO; i++)
            {
                StartNext();
            }

            for (int i = 0; i < lines.Length; i++)
            {
                AudioClip clip = await clipTasks[i];
                if (this == null || token != playbackToken) return;

                // Emotion transitions exactly when this line's audio starts.
                BeginLine(lines[i]);

                if (clip != null)
                {
                    // A notification for lip-sync and friends; the agent owns playback here.
                    OnAudioResponseReceived.Invoke(clip);
                    audioSource.clip = clip;
                    audioSource.Play();
                    await WaitForAudio(token);
                    if (this == null || token != playbackToken) return;
                }
            }

            FinishReply(token);
        }

        // Plays one clip for the whole reply while the lines drop in on top of it, holding the
        // "speaking" state until the clip ends so queued input doesn't cut in mid-sentence.
        private async Task PlayOneClipAndDrop(string fullText, ChatLine[] lines, int token)
        {
            AudioClip clip = await GenerateChatLineAudio(new ChatLine { text = fullText, emotion = lines[0].emotion });
            if (this == null || token != playbackToken) return;

            if (clip != null)
            {
                OnAudioResponseReceived.Invoke(clip);
                audioSource.clip = clip;
                audioSource.Play();
            }

            // The clip covers the gap, so no "typing…" cue while the character speaks aloud.
            await DropLines(lines, token, signalComposing: false);
            if (this == null || token != playbackToken) return;

            await WaitForAudio(token);
        }

        // Drops each line in as a message, pausing a reading-time estimate between them so the reply
        // arrives at a natural pace instead of all at once.
        private async Task DropLines(ChatLine[] lines, int token, bool signalComposing = true)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (this == null || token != playbackToken) return;
                BeginLine(lines[i]);
                if (i < lines.Length - 1)
                {
                    if (signalComposing) OnComposingNextLine.Invoke();
                    await WaitForSeconds(LineDelay(lines[i].text), token);
                }
            }
        }

        // Pause after a line, from its length, clamped so it never stalls or flickers.
        private static float LineDelay(string text)
        {
            float estimate = (string.IsNullOrEmpty(text) ? 0 : text.Length) / 15f;
            return Mathf.Clamp(estimate, 0.5f, 3f);
        }

        private void BeginLine(ChatLine line)
        {
            if (logToConsole)
            {
                Debug.Log($"[Neocortex] {name} [{line.emotion}]: {line.text}", this);
            }

            OnChatLineStarted.Invoke(line);
            OnEmotionChanged.Invoke(line.emotion);

            if (actionTrigger == ActionTrigger.WhenSpeechStarts)
            {
                RunPendingActions();
            }
        }

        private void FinishReply(int token)
        {
            if (token != playbackToken) return;

            state = ReplyState.Idle;
            OnReplyFinished.Invoke();

            if (actionTrigger == ActionTrigger.AfterReplySpoken)
            {
                RunPendingActions();
            }

            RearmMicrophone();
            DispatchNext();
        }

        private void DispatchNext()
        {
            if (pendingInputs.Count == 0) return;

            Action next = pendingInputs.Dequeue();
            state = ReplyState.WaitingForReply;
            next();
        }

        // ── Actions ─────────────────────────────────────────────────────────────────────────────

        // Queues a reply's actions in order, then runs them now or waits for the chosen trigger.
        private void CaptureActions(ChatResponse response)
        {
            if (response?.actions == null) return;

            foreach (ChatAction action in response.actions)
            {
                if (!string.IsNullOrEmpty(action?.name))
                {
                    actionQueue.Enqueue(action);
                }
            }

            // The deferred triggers wait for a playback event, which Off mode never raises, so the
            // queue would hang forever. Fire on arrival instead, once, with a heads-up.
            bool deferred = actionTrigger != ActionTrigger.WhenResponseReceived;

            if (deferred && chatLinesMode == ChatLinesMode.Off)
            {
                if (!warnedOffMode)
                {
                    warnedOffMode = true;
                    Debug.LogWarning("[Neocortex] Action Trigger fires on speech, but Chat Lines Mode is Off and raises no speech events. Running actions on arrival instead; set Single or Per-Line Audio.", this);
                }
                deferred = false;
            }

            if (!deferred)
            {
                RunPendingActions();
            }
        }

        // Safe to call more than once; the running/empty guards no-op.
        private void RunPendingActions()
        {
            if (!actionsRunning && actionQueue.Count > 0)
            {
                StartCoroutine(ProcessActions());
            }
        }

        private IEnumerator ProcessActions()
        {
            actionsRunning = true;

            while (actionQueue.Count > 0)
            {
                ChatAction action = actionQueue.Dequeue();

                if (actionHandlers.TryGetValue(action.name, out Func<ChatAction, IEnumerator> handler))
                {
                    if (logToConsole)
                    {
                        Debug.Log($"[Neocortex] {name} action: {action.name} {action.targetId}", this);
                    }

                    yield return handler(action);
                }
                else
                {
                    Debug.LogWarning($"[Neocortex] {name} asked for action '{action.name}', which has no handler. Register one with RegisterAction.", this);
                }
            }

            actionsRunning = false;
            OnActionsCompleted.Invoke();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────────────

        // True only when usage positively reports an empty balance; unknown/failed lookups return
        // false so a transient hiccup never blocks a paid feature.
        private async Task<bool> IsOutOfCredits(int token)
        {
            usageGate ??= new NeocortexUsageGate();
            ApiUsageResponse usage = await usageGate.GetUsageCached(characterId: characterID);
            if (this == null || token != playbackToken) return false;
            return usage != null && usage.status == UsageStatus.Empty;
        }

        private async Task WaitForAudio(int token)
        {
            while (this != null && token == playbackToken && audioSource != null && audioSource.isPlaying)
            {
                await Task.Yield();
            }
        }

        private async Task WaitForSeconds(float seconds, int token)
        {
            float endTime = Time.time + seconds;
            while (this != null && Time.time < endTime && token == playbackToken)
            {
                await Task.Yield();
            }
        }

        private static string JoinText(ChatLine[] lines) => string.Concat(lines.Select(l => l.text));

        private static bool NeedsAudio(ChatLinesMode mode)
        {
            return mode == ChatLinesMode.SingleAudio || mode == ChatLinesMode.PerLineAudio;
        }

        // ── Voice input ─────────────────────────────────────────────────────────────────────────

        // Hooks a microphone on this character straight into the agent, so an agent on its own can
        // hear the player and answer with no other component involved.
        private void BindVoiceInput()
        {
            voiceInput = GetComponent<AudioReceiver>() ?? GetComponentInChildren<AudioReceiver>(true);
            if (voiceInput == null) return;

            voiceInput.OnAudioRecorded.AddListener(AudioToAudio);
            RearmMicrophone();
        }

        // Voice-activity mode listens continuously; push-to-talk re-arms on its button instead.
        private void RearmMicrophone()
        {
            if (voiceInput != null && !voiceInput.usePushToTalk)
            {
                voiceInput.StartMicrophone();
            }
        }

        private AudioSource AddAudioSource()
        {
            AudioSource added = gameObject.AddComponent<AudioSource>();
            added.playOnAwake = false;
            return added;
        }

#if UNITY_EDITOR
        // Brings the audio pieces along when you switch INTO an audio mode, and only then, so any of
        // them you delete afterwards stays deleted.
        private void OnValidate()
        {
            if (chatLinesMode == lastEditorMode) return;

            bool cameFromAudio = NeedsAudio(lastEditorMode);
            lastEditorMode = chatLinesMode;

            if (NeedsAudio(chatLinesMode) && !cameFromAudio)
            {
                UnityEditor.EditorApplication.delayCall += AddAudioComponents;
            }
        }

        private void Reset()
        {
            lastEditorMode = chatLinesMode;

            if (NeedsAudio(chatLinesMode))
            {
                UnityEditor.EditorApplication.delayCall += AddAudioComponents;
            }
        }

        // Deferred, because Unity forbids adding components from inside OnValidate.
        private void AddAudioComponents()
        {
            if (this == null || Application.isPlaying) return;

            audioSource ??= GetComponent<AudioSource>();
            audioSource ??= AddAudioSource();

            // A microphone is a convenience here, not a requirement. Only ever one per hierarchy,
            // two would record the player twice.
            if (transform.root.GetComponentInChildren<AudioReceiver>(true) == null)
            {
                gameObject.AddComponent<NeocortexAudioReceiver>();
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
