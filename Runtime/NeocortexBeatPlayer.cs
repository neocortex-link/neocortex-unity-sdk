using System;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Neocortex
{
    /// <summary>
    /// Plays back the ordered per-emotion <see cref="ChatResponse.beats"/> of a reply. Attach next
    /// to a <see cref="NeocortexSmartAgent"/> (with <see cref="NeocortexSmartAgent.RequestBeats"/>
    /// enabled) and send player input through <see cref="SendText"/> / <see cref="SendAudio"/> so
    /// input arriving while the NPC is speaking is queued until the reply finishes.
    ///
    /// Two modes (see <see cref="BeatPlaybackMode"/>):
    /// <list type="bullet">
    /// <item><see cref="BeatPlaybackMode.TextReveal"/> (default) — beats appear as ordered messages
    /// and the emotion changes per beat. No extra credit cost.</item>
    /// <item><see cref="BeatPlaybackMode.PerBeatAudio"/> — OPT-IN: one audio request PER BEAT
    /// (1 audio credit each, up to 4x the normal cost) for progressive playback and audio-synced
    /// emotion changes. Use <see cref="SendText"/>, not TextToAudio, in this mode.</item>
    /// </list>
    /// Replies without beats (flag off, older server) play as a single beat, exactly as today.
    /// </summary>
    public class NeocortexBeatPlayer : MonoBehaviour
    {
        private const float FULL_AUDIO_TIMEOUT = 30f;

        /// <summary>Agent whose responses drive playback. Falls back to a component on this object if unset.</summary>
        [SerializeField] private NeocortexSmartAgent agent;

        /// <summary>Audio source the beat clips are played on. Required for any audio playback.</summary>
        [SerializeField] private AudioSource audioSource;

        [Tooltip("PerBeatAudio generates one clip per beat: 1 audio credit PER BEAT instead of 1 per reply.")]
        [SerializeField] private BeatPlaybackMode playbackMode = BeatPlaybackMode.TextReveal;

        [Tooltip("TextReveal mode only: request one audio clip for the full reply (1 credit) and pace the beat reveal to it.")]
        [SerializeField] private bool fullReplyAudio;

        [Tooltip("How many beat audio requests run at once in PerBeatAudio mode.")]
        [SerializeField, Range(1, 3)] private int maxConcurrentAudioRequests = 2;

        [Tooltip("Consult the usage API before spending per-beat credits: low balance falls back to a single clip, empty to text only.")]
        [SerializeField] private bool degradeOnLowCredits = true;

        [Tooltip("Seconds between beat reveals when there is no audio to pace them.")]
        [SerializeField] private float beatRevealDelay = 1f;

        [Space] public UnityEvent<Beat> OnBeatStarted;
        [Space] public UnityEvent<Emotions> OnEmotionChanged;
        [Space] public UnityEvent OnReplyFinished;

        private enum ReplyState { Idle, WaitingForReply, WaitingForFullAudio, Playing }

        private ReplyState state = ReplyState.Idle;
        private readonly Queue<Action> pendingInputs = new();
        private Beat[] pendingBeats;
        private int playbackToken;

        private NeocortexUsageGate usageGate;

        /// <summary>
        ///     Selects how beats are played. <see cref="BeatPlaybackMode.PerBeatAudio"/> costs
        ///     1 audio credit per beat (up to 4x a normal reply) — keep it off unless the extra
        ///     expressiveness is worth the credits.
        /// </summary>
        public BeatPlaybackMode PlaybackMode { get => playbackMode; set => playbackMode = value; }

        /// <summary>TextReveal mode only: request one full-reply clip (1 audio credit) and pace the reveal to it.</summary>
        public bool FullReplyAudio { get => fullReplyAudio; set => fullReplyAudio = value; }

        /// <summary>True while a reply is pending or its beats are still being played back.</summary>
        public bool IsSpeaking => state != ReplyState.Idle;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NeocortexSmartAgent>();

            if (agent == null)
            {
                Debug.LogError("[Neocortex] NeocortexBeatPlayer has no agent assigned.", this);
                return;
            }

            agent.OnChatResponseReceived.AddListener(HandleChatResponse);
            agent.OnAudioResponseReceived.AddListener(HandleFullReplyAudio);
            agent.OnRequestFailed.AddListener(HandleRequestFailed);
        }

        private void OnDestroy()
        {
            playbackToken++;

            if (agent != null)
            {
                agent.OnChatResponseReceived.RemoveListener(HandleChatResponse);
                agent.OnAudioResponseReceived.RemoveListener(HandleFullReplyAudio);
                agent.OnRequestFailed.RemoveListener(HandleRequestFailed);
            }
        }

        /// <summary>
        ///     Sends a text message, or queues it until the current reply finishes playing when the
        ///     NPC is still speaking. No barge-in: the current reply is never cut short.
        /// </summary>
        public void SendText(string message)
        {
            if (state != ReplyState.Idle)
            {
                pendingInputs.Enqueue(() => DispatchText(message));
                return;
            }

            DispatchText(message);
        }

        /// <summary>
        ///     Sends a recorded audio clip (transcribed server-side), or queues it until the current
        ///     reply finishes playing when the NPC is still speaking.
        /// </summary>
        public void SendAudio(AudioClip clip)
        {
            if (state != ReplyState.Idle)
            {
                pendingInputs.Enqueue(() => DispatchAudio(clip));
                return;
            }

            DispatchAudio(clip);
        }

        private void DispatchText(string message)
        {
            state = ReplyState.WaitingForReply;

            if (playbackMode == BeatPlaybackMode.TextReveal && fullReplyAudio)
            {
                agent.TextToAudio(message);
            }
            else
            {
                agent.TextToText(message);
            }
        }

        private void DispatchAudio(AudioClip clip)
        {
            state = ReplyState.WaitingForReply;

            if (playbackMode == BeatPlaybackMode.TextReveal && fullReplyAudio)
            {
                agent.AudioToAudio(clip);
            }
            else
            {
                agent.AudioToText(clip);
            }
        }

        private void HandleChatResponse(ChatResponse response)
        {
            // No beats (flag off or older server) → the whole reply is one beat, same as today.
            Beat[] beats = response.beats != null && response.beats.Length > 0
                ? response.beats
                : new[] { new Beat { text = response.message, emotion = response.emotion } };

            int token = ++playbackToken;

            if (playbackMode == BeatPlaybackMode.PerBeatAudio)
            {
                state = ReplyState.Playing;
                PlayPerBeatAudio(response, beats, token);
            }
            else if (fullReplyAudio)
            {
                state = ReplyState.WaitingForFullAudio;
                pendingBeats = beats;
                FullAudioWatchdog(token);
            }
            else
            {
                state = ReplyState.Playing;
                RevealBeats(beats, null, token);
            }
        }

        private void HandleFullReplyAudio(AudioClip clip)
        {
            // Only consume clips we are waiting for; otherwise playback stays developer-managed.
            if (state != ReplyState.WaitingForFullAudio || pendingBeats == null)
            {
                return;
            }

            Beat[] beats = pendingBeats;
            pendingBeats = null;
            state = ReplyState.Playing;
            RevealBeats(beats, clip, playbackToken);
        }

        private void HandleRequestFailed(string error)
        {
            switch (state)
            {
                case ReplyState.WaitingForReply:
                    // The chat call itself failed; unblock queued input.
                    state = ReplyState.Idle;
                    DispatchNextQueued();
                    break;

                case ReplyState.WaitingForFullAudio:
                    // The reply text is fine, only its voice failed — reveal without audio.
                    Beat[] beats = pendingBeats;
                    pendingBeats = null;
                    if (beats != null)
                    {
                        state = ReplyState.Playing;
                        RevealBeats(beats, null, playbackToken);
                    }
                    break;

                // Playing: per-beat audio failures are handled inline (the beat's text still
                // shows and the reply advances), so nothing to do here.
            }
        }

        private async void FullAudioWatchdog(int token)
        {
            await WaitForSeconds(FULL_AUDIO_TIMEOUT, token);
            if (this == null || token != playbackToken || state != ReplyState.WaitingForFullAudio || pendingBeats == null)
            {
                return;
            }

            Debug.LogWarning("[Neocortex] Full-reply audio did not arrive in time. Revealing beats as text only.");
            Beat[] beats = pendingBeats;
            pendingBeats = null;
            state = ReplyState.Playing;
            RevealBeats(beats, null, token);
        }

        private async void PlayPerBeatAudio(ChatResponse response, Beat[] beats, int token)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[Neocortex] NeocortexBeatPlayer has no AudioSource assigned. Revealing text only so no audio credits are spent.", this);
                RevealBeats(beats, null, token);
                return;
            }

            if (degradeOnLowCredits)
            {
                usageGate ??= new NeocortexUsageGate();
                ApiUsageResponse usage = await usageGate.GetUsageCached(characterId: agent.characterID);
                if (this == null || token != playbackToken) return;

                if (usage != null && usage.status == UsageStatus.Empty)
                {
                    Debug.LogWarning("[Neocortex] Credits are empty. Skipping audio for this reply.");
                    RevealBeats(beats, null, token);
                    return;
                }

                if (usage != null && usage.status == UsageStatus.Low)
                {
                    Debug.LogWarning("[Neocortex] Credits are low. Falling back to a single audio clip for this reply.");
                    AudioClip fullClip = await agent.GenerateBeatAudio(new Beat { text = response.message, emotion = beats[0].emotion });
                    if (this == null || token != playbackToken) return;
                    RevealBeats(beats, fullClip, token);
                    return;
                }
            }

            // Fetch clips with a small concurrency cap; play strictly in beat order, starting
            // beat 1 as soon as its clip returns while later beats keep synthesizing.
            int cap = Mathf.Clamp(maxConcurrentAudioRequests, 1, 3);
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
                AudioClip clip = await agent.GenerateBeatAudio(beats[index]);
                StartNext();
                return clip;
            }

            for (int i = 0; i < cap; i++)
            {
                StartNext();
            }

            for (int i = 0; i < beats.Length; i++)
            {
                AudioClip clip = await clipTasks[i];
                if (this == null || token != playbackToken) return;

                // Emotion transitions exactly when this beat's audio starts.
                BeginBeat(beats[i]);

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
                // clip == null (generation failed): the text is shown and the reply advances.
            }

            FinishReply(token);
        }

        private async void RevealBeats(Beat[] beats, AudioClip fullClip, int token)
        {
            state = ReplyState.Playing;

            float totalCharacters = 0f;
            foreach (Beat beat in beats)
            {
                totalCharacters += beat.text?.Length ?? 0;
            }

            bool hasAudio = fullClip != null && audioSource != null;
            if (hasAudio)
            {
                audioSource.clip = fullClip;
                audioSource.Play();
            }

            for (int i = 0; i < beats.Length; i++)
            {
                if (token != playbackToken) return;
                BeginBeat(beats[i]);

                if (hasAudio && totalCharacters > 0)
                {
                    // No timestamps from the server; approximate each beat's share of the clip
                    // by its share of the text.
                    float wait = fullClip.length * (beats[i].text?.Length ?? 0) / totalCharacters;
                    await WaitForSeconds(wait, token);
                }
                else if (i < beats.Length - 1)
                {
                    await WaitForSeconds(beatRevealDelay, token);
                }
            }

            if (this == null || token != playbackToken) return;
            FinishReply(token);
        }

        private void BeginBeat(Beat beat)
        {
            OnBeatStarted.Invoke(beat);
            OnEmotionChanged.Invoke(beat.emotion);
        }

        private void FinishReply(int token)
        {
            if (token != playbackToken) return;

            state = ReplyState.Idle;
            OnReplyFinished.Invoke();
            DispatchNextQueued();
        }

        private void DispatchNextQueued()
        {
            if (pendingInputs.Count > 0)
            {
                pendingInputs.Dequeue().Invoke();
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
    }
}
