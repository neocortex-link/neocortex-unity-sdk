using System;
using UnityEngine;

namespace Neocortex
{
    public class NeocortexAudioReceiver : AudioReceiver
    {
        private const int FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 64;
        private const int AMPLITUDE_MULTIPLIER = 10;
        
        private AudioClip audioClip;
        private bool initialized;
        
        public string SelectedMicrophone { get; set; }
        public bool IsUserSpeaking { get; private set; }
        
        [SerializeField, Range(0, 1)] private float amplitudeThreshold = 0.1f;
        [SerializeField] private float maxWaitTime = 1f;
        
        public override void StartMicrophone()
        {
            try
            {
                SelectedMicrophone = NeocortexMicrophone.devices[PlayerPrefs.GetInt(MIC_INDEX_KEY, 0)];
                audioClip = NeocortexMicrophone.Start(SelectedMicrophone, true, 999, FREQUENCY);
                initialized = true;
            }
            catch (Exception e)
            {
                OnRecordingFailed?.Invoke(e.Message);
            }
        }
        
        public override void StopMicrophone()
        {
            NeocortexMicrophone.End(SelectedMicrophone);
            initialized = false;
            IsUserSpeaking = false;
            AudioRecorded();
        }
        
        private void Update()
        {
            if (!initialized) return;
            
            UpdateAmplitude();

            if (UsePushToTalk) return;
            
            if(!IsUserSpeaking && Amplitude > amplitudeThreshold)
            {
                IsUserSpeaking = true;
            }
            
            if (IsUserSpeaking)
            {
                if (Amplitude < amplitudeThreshold)
                {
                    ElapsedWaitTime += Time.deltaTime;
                
                    if(ElapsedWaitTime >= maxWaitTime)
                    {
                        ElapsedWaitTime = 0;
                        StopMicrophone();
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
            if (!trimmed)
            {
                StartMicrophone();
            }
            else
            {
                OnAudioRecorded?.Invoke(trimmed);
            }
        }
        
        private void UpdateAmplitude()
        {
            int clipPosition = NeocortexMicrophone.GetPosition(SelectedMicrophone);
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
                NeocortexMicrophone.End(SelectedMicrophone);
            }
        }
    }
}
