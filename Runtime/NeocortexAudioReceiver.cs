using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neocortex
{
    public class NeocortexAudioReceiver : MonoBehaviour
    {
        private const int FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 64;
        private const int AMPLITUDE_MULTIPLIER = 10;
        
        private AudioClip audioClip;
        private bool initialized;
        
        public string SelectedMicrophone { get; set; }
        public float Amplitude { get; private set; }
        public bool IsRecording { get; private set; }
        public float ElapsedWaitTime { get; set; }

        public bool UsePushToTalk => usePushToTalk;

        [HideInInspector] public UnityEvent<AudioClip> OnAudioRecorded;
        
        [SerializeField, Range(0, 1)] private float amplitudeTreshold = 0.1f;
        [SerializeField] private float maxWaitTime = 1f;
        [SerializeField] private bool usePushToTalk;
        
        public void StartMicrophone()
        {
            audioClip = Microphone.Start(SelectedMicrophone, true, 999, FREQUENCY);
            initialized = true;
        }
        
        public void StopMicrophone()
        {
            Microphone.End(SelectedMicrophone);
            IsRecording = false;
            if (usePushToTalk)
            {
                AudioRecorded();
            }
        }
        
        private void Update()
        {
            if (!initialized) return;
            
            int position = Microphone.GetPosition(SelectedMicrophone);
            UpdateAmplitude(position, audioClip);

            if (usePushToTalk) return;
            
            if(!IsRecording && Amplitude > amplitudeTreshold)
            {
                IsRecording = true;
            }
            
            if (IsRecording)
            {
                if (Amplitude < amplitudeTreshold)
                {
                    ElapsedWaitTime += Time.deltaTime;
                
                    if(ElapsedWaitTime >= maxWaitTime)
                    {
                        IsRecording = false;
                        ElapsedWaitTime = 0;
                        AudioRecorded();
                    }
                }
                else
                {
                    ElapsedWaitTime = 0;
                }
            }
        }

        private void AudioRecorded()
        {
            AudioClip trimmed = audioClip.Trim();
                        
            if (trimmed.samples > FREQUENCY)
            {
                Microphone.End(SelectedMicrophone);
                OnAudioRecorded?.Invoke(trimmed);
            }
            else
            {
                Debug.Log("Audio too short");
            }
        }
        
        private void UpdateAmplitude(int clipPosition, AudioClip audioClip)
        {
            int startPosition = Mathf.Max(0, clipPosition - AUDIO_SAMPLE_WINDOW);
            float[] audioSamples = new float[AUDIO_SAMPLE_WINDOW];
            audioClip.GetData(audioSamples, startPosition);
        
            float sum = 0;
            for (int i = 0; i < AUDIO_SAMPLE_WINDOW; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }
        
            Amplitude = Mathf.Clamp01(sum / AUDIO_SAMPLE_WINDOW * AMPLITUDE_MULTIPLIER);
        }

        private void OnDestroy()
        {
            if (initialized)
            {
                Microphone.End(SelectedMicrophone);
            }
        }
    }
}
