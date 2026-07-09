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
    /// Optionally set <see cref="BeatsMode"/> to have replies delivered as ordered per-emotion
    /// "beats" instead of one block of text:
    /// <list type="bullet">
    /// <item><see cref="Data.BeatsMode.Off"/> — normal single reply (default, unchanged).</item>
    /// <item><see cref="Data.BeatsMode.TextAndEmotion"/> — reveal each beat via <see cref="OnBeatStarted"/>
    /// and change emotion via <see cref="OnEmotionChanged"/>. No extra credit cost.</item>
    /// <item><see cref="Data.BeatsMode.ExpressiveAudio"/> — also voice each beat in its own emotion.
    /// WARNING: this costs ~1 audio credit PER BEAT (up to 4x a normal reply). Assign an
    /// <c>AudioSource</c> and it degrades automatically when credits run low.</item>
    /// </list>
    /// In any beats mode, calling a send method while the character is still speaking queues the
    /// input and submits it once the current reply finishes (no barge-in).
    /// </summary>
    public class NeocortexSmartAgent : MonoBehaviour
    {
        // How many beat audio clips are generated at once in Expressive Audio mode.
        private const int MAX_CONCURRENT_AUDIO = 2;
        // Seconds between beat reveals when there is no audio pacing them.
        private const float BEAT_REVEAL_DELAY = 1f;

        private ApiRequest apiRequest;

        public string characterID;

        [Tooltip("Off: normal single reply.\n" +
                 "Text And Emotion: reveal each beat as its own message and change emotion per beat (no extra cost).\n" +
                 "Expressive Audio: also voice each beat — WARNING: costs ~1 audio credit PER BEAT (up to 4x).")]
        [SerializeField] private BeatsMode beatsMode = BeatsMode.Off;

        [Tooltip("Required for Expressive Audio mode; beat clips play here.")]
        [SerializeField] private AudioSource audioSource;

        [Space] public UnityEvent<ChatResponse> OnChatResponseReceived;
        [Space] public UnityEvent<AudioClip> OnAudioResponseReceived;
        [Space] public UnityEvent<string> OnTranscriptionReceived;
        [Space] public UnityEvent<string> OnRequestFailed;
        [Space] public UnityEvent<Message[]> OnChatHistoryReceived;

        [Header("Beats")]
        [Tooltip("Raised when a beat starts (its text is ready to show).")]
        [Space] public UnityEvent<Beat> OnBeatStarted;
        [Tooltip("Raised with each beat's emotion, when the beat starts. Drive animation here.")]
        [Space] public UnityEvent<Emotions> OnEmotionChanged;
        [Tooltip("Raised once the whole reply has finished playing.")]
        [Space] public UnityEvent OnReplyFinished;

        private enum ReplyState { Idle, WaitingForReply, Playing }

        private ReplyState state = ReplyState.Idle;
        private readonly Queue<Action> pendingInputs = new();
        private int playbackToken;
        private NeocortexUsageGate usageGate;

        /// <summary>
        ///     Selects how replies are delivered. <see cref="Data.BeatsMode.ExpressiveAudio"/> costs
        ///     ~1 audio credit per beat (up to 4x a normal reply); leave it off unless the extra
        ///     expressiveness is worth the credits.
        /// </summary>
        public BeatsMode BeatsMode
        {
            get => beatsMode;
            set
            {
                beatsMode = value;
                if (apiRequest != null)
                {
                    apiRequest.RequestBeats = value != BeatsMode.Off;
                }
            }
        }

        /// <summary>True while a reply is pending or its beats are still being played back.</summary>
        public bool IsSpeaking => state != ReplyState.Idle;

        private void Awake()
        {
            apiRequest = new ApiRequest();
            apiRequest.RequestBeats = beatsMode != BeatsMode.Off;
            apiRequest.OnChatResponseReceived += HandleChatResponse;
            apiRequest.OnAudioResponseReceived += OnAudioResponseReceived.Invoke;
            apiRequest.OnTranscriptionReceived += OnTranscriptionReceived.Invoke;
            apiRequest.OnChatHistoryReceived += OnChatHistoryReceived.Invoke;
            apiRequest.OnRequestFailed += HandleRequestFailed;
        }

        private void OnDestroy()
        {
            playbackToken++; // cancel any in-flight beat playback
        }

        public void TextToText(string message)
        {
            Dispatch(() => apiRequest.Send<string, string>(characterID, message));
        }

        public void TextToAudio(string message)
        {
            // In beats mode, audio is produced per beat (or not at all), so route to text-only
            // to avoid also paying for a separate full-reply clip.
            if (beatsMode == BeatsMode.Off)
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
            if (beatsMode == BeatsMode.Off)
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
        ///     Generates one audio clip for a single beat, voiced in that beat's emotion.
        ///     IMPORTANT: each call costs 1 audio credit, so voicing every beat of a reply costs
        ///     up to 4 credits instead of 1. Returns null when generation fails.
        /// </summary>
        public Task<AudioClip> GenerateBeatAudio(Beat beat)
        {
            return apiRequest.GenerateAudio(characterID, beat.text, beat.emotion.ToString().ToUpper());
        }

        // Sends immediately in Off mode (today's fire-and-forget behavior). In a beats mode, queues
        // the input if the character is still speaking so nothing is submitted mid-reply.
        private void Dispatch(Action send)
        {
            if (beatsMode == BeatsMode.Off)
            {
                send();
                return;
            }

            if (state != ReplyState.Idle)
            {
                pendingInputs.Enqueue(send);
                BeatLog($"Input received while speaking — queued (pending: {pendingInputs.Count}).");
                return;
            }

            state = ReplyState.WaitingForReply;
            send();
        }

        private void HandleChatResponse(ChatResponse response)
        {
            OnChatResponseReceived.Invoke(response);

            if (beatsMode == BeatsMode.Off)
            {
                return;
            }

            // No beats (older server, or none returned) → treat the whole reply as one beat.
            Beat[] beats = response.beats != null && response.beats.Length > 0
                ? response.beats
                : new[] { new Beat { text = response.message, emotion = response.emotion } };

            if (response.beats != null && response.beats.Length > 0)
            {
                string joined = string.Concat(response.beats.Select(b => b.text));
                BeatLog($"Reply: {beats.Length} beat(s), mode={beatsMode}. Concat==response: {joined == response.message}");
            }
            else
            {
                BeatLog($"Reply had no beats (mode={beatsMode}) — playing as a single beat.");
            }

            state = ReplyState.Playing;
            int token = ++playbackToken;

            if (beatsMode == BeatsMode.ExpressiveAudio)
            {
                PlayExpressive(response, beats, token);
            }
            else
            {
                RevealBeats(beats, token);
            }
        }

        private void HandleRequestFailed(string error)
        {
            OnRequestFailed.Invoke(error);

            // Only a failure of the chat request itself (before playback) should unblock the queue.
            // Per-beat audio failures during playback are handled inline (text still shows, advance).
            if (beatsMode != BeatsMode.Off && state == ReplyState.WaitingForReply)
            {
                BeatLog($"Chat request failed before playback: {error}. Unblocking queue.");
                state = ReplyState.Idle;
                playbackToken++;
                DispatchNext();
            }
        }

        // Reveals each beat in order with a small delay, driving emotion per beat. No audio cost.
        private async void RevealBeats(Beat[] beats, int token)
        {
            for (int i = 0; i < beats.Length; i++)
            {
                if (this == null || token != playbackToken) return;
                BeginBeat(i, beats[i]);
                if (i < beats.Length - 1)
                {
                    await WaitForSeconds(BEAT_REVEAL_DELAY, token);
                }
            }

            if (this == null || token != playbackToken) return;
            FinishReply(token);
        }

        private async void PlayExpressive(ChatResponse response, Beat[] beats, int token)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[Neocortex] Beats Mode is Expressive Audio but no AudioSource is assigned. Playing text only (no audio credits spent).", this);
                RevealBeats(beats, token);
                return;
            }

            // Credit-aware: don't burn N credits per reply when the balance is low or empty.
            usageGate ??= new NeocortexUsageGate();
            ApiUsageResponse usage = await usageGate.GetUsageCached(characterId: characterID);
            if (this == null || token != playbackToken) return;

            if (usage != null && usage.status == UsageStatus.Empty)
            {
                BeatLog("Credits empty — degrading to text only.");
                Debug.LogWarning("[Neocortex] Credits are empty. Playing beats as text only.");
                RevealBeats(beats, token);
                return;
            }

            if (usage != null && usage.status == UsageStatus.Low)
            {
                BeatLog("Credits low — degrading to a single audio clip.");
                Debug.LogWarning("[Neocortex] Credits are low. Voicing this reply with one clip instead of per-beat.");
                AudioClip single = await GenerateBeatAudio(new Beat { text = response.message, emotion = beats[0].emotion });
                if (this == null || token != playbackToken) return;
                if (single != null)
                {
                    audioSource.clip = single;
                    audioSource.Play();
                }
                RevealBeats(beats, token);
                return;
            }

            BeatLog($"Expressive audio: generating {beats.Length} clip(s), up to {MAX_CONCURRENT_AUDIO} at once.");

            // Fetch clips with a small concurrency cap; play strictly in beat order, starting
            // beat 1 as soon as its clip returns while later beats keep synthesizing.
            Task<AudioClip>[] clipTasks = new Task<AudioClip>[beats.Length];
            int nextToStart = 0;

            void StartNext()
            {
                if (nextToStart < beats.Length)
                {
                    int index = nextToStart++;
                    clipTasks[index] = FetchClip(index);
                }
            }

            async Task<AudioClip> FetchClip(int index)
            {
                BeatLog($"Requesting audio for beat {index + 1} [{beats[index].emotion}].");
                AudioClip clip = await GenerateBeatAudio(beats[index]);
                BeatLog(clip != null
                    ? $"Audio for beat {index + 1} ready."
                    : $"Audio for beat {index + 1} FAILED — showing its text only.");
                StartNext();
                return clip;
            }

            for (int i = 0; i < MAX_CONCURRENT_AUDIO; i++)
            {
                StartNext();
            }

            for (int i = 0; i < beats.Length; i++)
            {
                AudioClip clip = await clipTasks[i];
                if (this == null || token != playbackToken) return;

                // Emotion transitions exactly when this beat's audio starts.
                BeginBeat(i, beats[i]);

                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    while (token == playbackToken && audioSource != null && audioSource.isPlaying)
                    {
                        await Task.Yield();
                    }
                    if (this == null || token != playbackToken) return;
                }
                // clip == null (generation failed): text was shown, just advance to the next beat.
            }

            FinishReply(token);
        }

        private void BeginBeat(int index, Beat beat)
        {
            BeatLog($"Beat {index + 1} [{beat.emotion}] \"{Preview(beat.text)}\"");
            OnBeatStarted.Invoke(beat);
            OnEmotionChanged.Invoke(beat.emotion);
        }

        private void FinishReply(int token)
        {
            if (token != playbackToken) return;

            state = ReplyState.Idle;
            BeatLog("Reply finished.");
            OnReplyFinished.Invoke();
            DispatchNext();
        }

        private void DispatchNext()
        {
            if (pendingInputs.Count == 0) return;

            BeatLog($"Dispatching queued input ({pendingInputs.Count - 1} still waiting).");
            Action next = pendingInputs.Dequeue();
            state = ReplyState.WaitingForReply;
            next();
        }

        private async Task WaitForSeconds(float seconds, int token)
        {
            float endTime = Time.time + seconds;
            while (this != null && Time.time < endTime && token == playbackToken)
            {
                await Task.Yield();
            }
        }

        // ── Temporary beats diagnostics. To remove: delete BeatLog + Preview and their call sites
        //    (grep "[Neocortex][Beats]"). ─────────────────────────────────────────────────────────
        private static void BeatLog(string message)
        {
            Debug.Log($"[Neocortex][Beats] {message}");
        }

        private static string Preview(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= 40 ? text : text.Substring(0, 40) + "…";
        }
        // ── End temporary beats diagnostics. ───────────────────────────────────────────────────

        [Obsolete("This method is replaced by NeocortexSessionManager.GetSessionID")]
        public string GetSessionID()
        {
            return NeocortexSessionManager.GetSessionID(characterID);
        }

        [Obsolete("This method is replaced by NeocortexSessionManager.CleanSessionID")]
        public void CleanSessionID()
        {
            NeocortexSessionManager.CleanSessionID(characterID);
        }
    }
}
