using System;
using System.Linq;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Neocortex
{
    /// <summary>
    /// The main component for talking to a Neocortex character. Add it to a GameObject, set the
    /// Character ID, and call <see cref="TextToText"/> / <see cref="AudioToAudio"/> etc.
    ///
    /// Set <see cref="ChatLinesMode"/> to deliver replies as ordered <b>chat lines</b>, chunks that
    /// drop in one after another as separate messages (with emotion per line) instead of one block
    /// of text. The message drop is the same in every mode; the mode only decides the audio:
    /// <list type="bullet">
    /// <item><see cref="Data.ChatLinesMode.Off"/>, one whole reply (default, unchanged).</item>
    /// <item><see cref="Data.ChatLinesMode.Text"/>, chat lines drop in, no audio. No extra cost.</item>
    /// <item><see cref="Data.ChatLinesMode.SingleAudio"/>, lines drop in over ONE voice clip (1 credit).</item>
    /// <item><see cref="Data.ChatLinesMode.PerLineAudio"/>, each line is voiced separately, in order.
    /// WARNING: ~1 audio credit PER line (up to 4x). Needs an <c>AudioSource</c>; degrades on its own
    /// when credits run low.</item>
    /// </list>
    /// Listen to <see cref="OnChatLineStarted"/> to show each message and <see cref="OnEmotionChanged"/>
    /// to drive animation. Input sent while the character is still speaking is queued and submitted
    /// when the reply finishes (no barge-in). Audio modes need an <see cref="AudioSource"/> assigned.
    /// </summary>
    public class NeocortexSmartAgent : MonoBehaviour
    {
        // How many line clips are generated at once in Per-Line Audio mode.
        private const int MAX_CONCURRENT_AUDIO = 2;

        private ApiRequest apiRequest;

        public string characterID;

        /// <summary>
        ///     The character (project) id this agent talks to. A <see cref="NeocortexGroupDirector"/>
        ///     reads it to build the cast for a group turn.
        /// </summary>
        public string CharacterID
        {
            get => characterID;
            set => characterID = value;
        }

        [Tooltip("Off: one normal reply.\n" +
                 "Text: chat lines drop in as messages with emotion, no extra cost.\n" +
                 "Single Audio: same, plus ONE voice clip for the whole reply (1 credit).\n" +
                 "Per-Line Audio: same, but each line is voiced separately, WARNING: ~1 credit PER line (up to 4x).")]
        [SerializeField] private ChatLinesMode chatLinesMode = ChatLinesMode.Off;

        [Tooltip("Required for Single Audio and Per-Line Audio modes; line clips play here.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Fetch and replay this character's stored conversation via OnChatHistoryReceived when the scene starts.")]
        [SerializeField] private bool loadHistoryOnStart;

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
        [Tooltip("Text mode: raised during the pause BEFORE the next line, so a 'typing…' indicator can fill the gap. Not raised while a voice clip is playing.")]
        [Space] public UnityEvent OnComposingNextLine = new();
        [Tooltip("Raised once the whole reply has finished playing.")]
        [Space] public UnityEvent OnReplyFinished = new();

        private enum ReplyState { Idle, WaitingForReply, Playing }

        private ReplyState state = ReplyState.Idle;
        private readonly Queue<Action> pendingInputs = new();
        private int playbackToken;
        private NeocortexUsageGate usageGate;

        /// <summary>
        ///     How replies are delivered. <see cref="Data.ChatLinesMode.PerLineAudio"/> costs ~1 audio
        ///     credit per line (up to 4x a normal reply); <see cref="Data.ChatLinesMode.SingleAudio"/>
        ///     costs 1. Leave audio modes off unless the extra expressiveness is worth the credits.
        /// </summary>
        public ChatLinesMode ChatLinesMode
        {
            get => chatLinesMode;
            set => chatLinesMode = value;
        }

        /// <summary>
        ///     Audio source the audio chat-lines modes play through. Assign in the inspector, or set
        ///     it from code before the first reply so a sample can hand the agent its own source.
        /// </summary>
        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }

        /// <summary>True while a reply is pending or its chat lines are still being played back.</summary>
        public bool IsSpeaking => state != ReplyState.Idle;

        private void Awake()
        {
            // Serialized-first, with a friendly fallback: an AudioSource on the same GameObject
            // counts as assigned.
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            apiRequest = new ApiRequest();
            // The character perceives the world from where it stands.
            apiRequest.PerceptionOrigin = transform;
            apiRequest.PerceptionRadius = 0;
            apiRequest.MaxPerceivedEntities = 10;
            apiRequest.OnChatResponseReceived += HandleChatResponse;
            apiRequest.OnAudioResponseReceived += OnAudioResponseReceived.Invoke;
            apiRequest.OnTranscriptionReceived += OnTranscriptionReceived.Invoke;
            apiRequest.OnChatHistoryReceived += OnChatHistoryReceived.Invoke;
            apiRequest.OnRequestFailed += HandleRequestFailed;
        }

        private void Start()
        {
            // Only when a session actually exists, a fresh install has nothing to load and an
            // empty session id would be rejected by the server.
            if (loadHistoryOnStart && NeocortexSessionManager.GetSessionID(characterID) != "")
            {
                GetChatHistory();
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
            // In a chat-lines mode the agent produces audio itself (single or per line), so request
            // text only, otherwise we'd also pay for a separate full-reply clip.
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

        /// <summary>
        ///     Fetches this character's history with pagination. Pass the previous result's
        ///     <see cref="ApiChatHistory.nextCursor"/> as <paramref name="before"/> to load older
        ///     messages (scroll-up). Returns the page + its cursor; does not raise the event.
        /// </summary>
        public Task<ApiChatHistory> RequestChatHistory(int limit = 10, string before = null)
        {
            return apiRequest.RequestChatHistory(NeocortexSessionManager.GetSessionID(characterID), limit, before);
        }

        /// <summary>
        ///     Generates one audio clip for a chat line, voiced in its emotion.
        ///     IMPORTANT: each call costs 1 audio credit.
        /// </summary>
        public Task<AudioClip> GenerateChatLineAudio(ChatLine line)
        {
            return apiRequest.GenerateAudio(characterID, line.text, line.emotion.ToString().ToUpper());
        }

        // Sends immediately in Off mode (fire-and-forget, as before). In a chat-lines mode, queues
        // the input while the character is still speaking so nothing is submitted mid-reply.
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

            if (chatLinesMode == ChatLinesMode.Off)
            {
                return;
            }

            // No lines (older server, or none returned) → treat the whole reply as one line.
            ChatLine[] lines = response.lines != null && response.lines.Length > 0
                ? response.lines
                : new[] { new ChatLine { text = response.message, emotion = response.emotion } };

            state = ReplyState.Playing;
            int token = ++playbackToken;
            _ = PlayReply(lines, token);
        }

        // Plays a reply's lines according to the current ChatLinesMode. Returns a Task so the group
        // director can await one character's whole turn before the next speaks; the single-character
        // path fires it and forgets it (behaviour unchanged from the old async-void switch).
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
                default: // Text
                    await PlayTextOnly(lines, token);
                    break;
            }
        }

        /// <summary>
        ///     Plays a reply produced elsewhere, a <see cref="NeocortexGroupDirector"/> turn, through
        ///     this agent, so the character speaks with its own chat-line playback, emotion events and
        ///     audio source (the personality stays "attached" to this GameObject). Awaitable, so the
        ///     director can let one character finish speaking before the next begins. Sends no request
        ///     of its own. Honors <see cref="ChatLinesMode"/> exactly like a self-driven reply.
        /// </summary>
        public async Task Speak(GroupMessage message)
        {
            ChatResponse response = ApiRequest.ToChatResponse(message, null);
            OnChatResponseReceived.Invoke(response);

            if (chatLinesMode == ChatLinesMode.Off)
            {
                // Off mode has no line-by-line playback; the whole reply is delivered via the event.
                return;
            }

            ChatLine[] lines = response.lines != null && response.lines.Length > 0
                ? response.lines
                : new[] { new ChatLine { text = response.message, emotion = response.emotion } };

            state = ReplyState.Playing;
            int token = ++playbackToken;
            await PlayReply(lines, token);

            await WaitForSeconds(1f, playbackToken);
            
            
        }

        private void HandleRequestFailed(string error)
        {
            OnRequestFailed.Invoke(error);

            // Only a failure of the chat request itself (before playback) should unblock the queue.
            // Per-line audio failures during playback are handled inline (text still shows, advance).
            if (chatLinesMode != ChatLinesMode.Off && state == ReplyState.WaitingForReply)
            {
                state = ReplyState.Idle;
                playbackToken++;
                DispatchNext();
            }
        }

        // ── Playback ────────────────────────────────────────────────────────────────────────────

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

            // Credit-aware degrade, invisible to the developer: empty → text only, low → one clip.
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

            // Fetch clips with a small concurrency cap; play strictly in line order, starting
            // line 1 as soon as its clip returns while later lines keep synthesizing.
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
                BeginLine(i, lines[i]);

                if (clip != null)
                {
                    // Surface the clip like Off-mode audio does, so a unified audio handler / lip-sync
                    // can observe it. The agent still owns playback here (it drives the line cadence),
                    // so listeners should treat this as a notification, not a cue to play it again.
                    OnAudioResponseReceived.Invoke(clip);
                    audioSource.clip = clip;
                    audioSource.Play();
                    await WaitForAudio(token);
                    if (this == null || token != playbackToken) return;
                }
                // clip == null (generation failed): text was shown, just advance to the next line.
            }

            FinishReply(token);
        }

        // Plays one clip for the whole reply while the lines drop in on top of it, then holds the
        // "speaking" state until the clip finishes so queued input doesn't cut in mid-sentence.
        private async Task PlayOneClipAndDrop(string fullText, ChatLine[] lines, int token)
        {
            AudioClip clip = await GenerateChatLineAudio(new ChatLine { text = fullText, emotion = lines[0].emotion });
            if (this == null || token != playbackToken) return;

            if (clip != null)
            {
                // Surface the clip like Off-mode audio does (notification only, the agent plays it).
                OnAudioResponseReceived.Invoke(clip);
                audioSource.clip = clip;
                audioSource.Play();
            }

            // The clip covers the gap here, no "typing…" cue while the character is speaking aloud.
            await DropLines(lines, token, signalComposing: false);
            if (this == null || token != playbackToken) return;

            await WaitForAudio(token);
        }

        // Drops each chat line in as a message, waiting a reading-time estimate between them so the
        // reply arrives at a natural pace instead of dumping in all at once. When there is no audio
        // covering the gap (text mode), signal OnComposingNextLine so the UI can show a "typing…" cue.
        private async Task DropLines(ChatLine[] lines, int token, bool signalComposing = true)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (this == null || token != playbackToken) return;
                BeginLine(i, lines[i]);
                if (i < lines.Length - 1)
                {
                    if (signalComposing) OnComposingNextLine.Invoke();
                    await WaitForSeconds(LineDelay(lines[i].text), token);
                }
            }
        }

        // Estimated pause after a chat line, from its length: a longer message earns a longer pause,
        // clamped so it never stalls or flickers. Audio modes pace by clip length; this shapes text.
        private float LineDelay(string text)
        {
            float estimate = (string.IsNullOrEmpty(text) ? 0 : text.Length) / Mathf.Max(1f, 15);
            return Mathf.Clamp(estimate, 0.5f, 3f);
        }

        private void BeginLine(int index, ChatLine line)
        {
            OnChatLineStarted.Invoke(line);
            OnEmotionChanged.Invoke(line.emotion);
        }

        private void FinishReply(int token)
        {
            if (token != playbackToken) return;

            state = ReplyState.Idle;
            OnReplyFinished.Invoke();
            DispatchNext();
        }

        private void DispatchNext()
        {
            if (pendingInputs.Count == 0) return;

            Action next = pendingInputs.Dequeue();
            state = ReplyState.WaitingForReply;
            next();
        }

        // Returns true only when the usage API positively reports an empty balance; unknown/failed
        // lookups return false so a transient usage hiccup never blocks a paid feature.
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
    }
}
