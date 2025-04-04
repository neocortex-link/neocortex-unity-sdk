using UnityEngine;
using UnityEngine.Events;

namespace Neocortex
{
    public abstract class AudioReceiver: MonoBehaviour
    {
        public const string MIC_INDEX_KEY = "neocortex-mic-index";
        
        [SerializeField] private bool usePushToTalk;
        public bool UsePushToTalk { get => usePushToTalk; protected set => usePushToTalk = value; }
        public float Amplitude { get; protected set; }
        public float ElapsedWaitTime { get; protected set; }
        
        public abstract void StartMicrophone();
        public abstract void StopMicrophone();
        
        [HideInInspector] public UnityEvent<AudioClip> OnAudioRecorded;
        [HideInInspector] public UnityEvent<string> OnRecordingFailed;
    }
}
