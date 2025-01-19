using UnityEngine;
using UnityEngine.Events;

namespace Neocortex
{
    public abstract class AudioReceiver: MonoBehaviour
    {
        public bool UsePushToTalk { get; protected set; }
        public float Amplitude { get; protected set; }
        public float ElapsedWaitTime { get; protected set; }
        
        public abstract void StartMicrophone();
        public abstract void StopMicrophone();
        
        [HideInInspector] public UnityEvent<AudioClip> OnAudioRecorded;
        [HideInInspector] public UnityEvent<string> OnRecordingFailed;
    }
}
