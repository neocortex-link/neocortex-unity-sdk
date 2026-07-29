using UnityEngine;
using UnityEngine.Events;

namespace Neocortex
{
    public abstract class AudioReceiver: MonoBehaviour
    {
        public const string MIC_INDEX_KEY = "neocortex-mic-index";

        [Tooltip("Hold-to-record instead of voice activity.")]
        public bool usePushToTalk;
        [Tooltip("Voice-activity mode: amplitude above this counts as speech.")]
        [Range(0, 1)] public float amplitudeThreshold = 0.1f;
        [Tooltip("Voice-activity mode: seconds of silence before the recording is finished.")]
        public float maxWaitTime = 1f;

        public float Amplitude { get; protected set; }
        public float ElapsedWaitTime { get; protected set; }

        /// <summary>True while the player's voice is being recorded.</summary>
        public bool IsUserSpeaking { get; protected set; }

        /// <summary>True while the microphone is open and able to hear the player.</summary>
        public bool IsListening { get; protected set; }

        public abstract void StartMicrophone();
        public abstract void StopMicrophone();

        [HideInInspector] public UnityEvent<AudioClip> OnAudioRecorded = new();
        [HideInInspector] public UnityEvent<string> OnRecordingFailed = new();

        // Shared tail of every recording: trim the silence; an empty recording restarts the
        // mic instead of surfacing a blank clip.
        protected void EmitRecordedClip(AudioClip raw)
        {
            AudioClip trimmed = raw.Trim();
            if (!trimmed)
            {
                // Nothing was said. Voice activity keeps listening; push-to-talk must not reopen the
                // mic behind a released button.
                if (!usePushToTalk)
                {
                    StartMicrophone();
                }
            }
            else
            {
                OnAudioRecorded?.Invoke(trimmed);
            }
        }
    }
}
