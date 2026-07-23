using UnityEngine;
using UnityEngine.Events;

namespace Neocortex
{
    public abstract class AudioReceiver: MonoBehaviour
    {
        public const string MIC_INDEX_KEY = "neocortex-mic-index";

        [SerializeField] private bool usePushToTalk;
        [Tooltip("Voice-activity mode: amplitude above this counts as speech.")]
        [SerializeField, Range(0, 1)] protected float amplitudeThreshold = 0.1f;
        [Tooltip("Voice-activity mode: seconds of silence before the recording is finished.")]
        [SerializeField] protected float maxWaitTime = 1f;

        public bool UsePushToTalk { get => usePushToTalk; set => usePushToTalk = value; }
        public float AmplitudeThreshold { get => amplitudeThreshold; set => amplitudeThreshold = value; }
        public float MaxWaitTime { get => maxWaitTime; set => maxWaitTime = value; }
        public float Amplitude { get; protected set; }
        public float ElapsedWaitTime { get; protected set; }

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
                StartMicrophone();
            }
            else
            {
                OnAudioRecorded?.Invoke(trimmed);
            }
        }
    }
}
