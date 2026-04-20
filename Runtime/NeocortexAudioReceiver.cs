using System;
using UnityEngine;

namespace Neocortex
{
    public class NeocortexAudioReceiver : AudioReceiver
    {
        private const int FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 256;
        private const int AMPLITUDE_MULTIPLIER = 10;
        
        private const float ZCR_TRESHOLD = 0.2f;
        private const float MIN_AMPLITUDE_RISE = 0.04f;
        
        private AudioClip audioClip;
        private bool initialized;

        public string SelectedMicrophone { get; set; }
        public bool IsUserSpeaking { get; private set; }

        [SerializeField, Range(0, 1)] private float amplitudeThreshold = 0.05f;
        [SerializeField] private float minSpeechDuration = 0.25f;
        [SerializeField] private float maxWaitTime = 1f;
        
        private float zcrValue = 0;
        private float previousAmplitude = 0;
        private float rollingAverage = 0;
        private const float smoothingFactor = 0.95f;
        private float speechStartTime = -1f;

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

            float ampDelta = Amplitude - previousAmplitude;

            if (UsePushToTalk) return;

            if (!IsUserSpeaking &&
                Amplitude > amplitudeThreshold &&
                ampDelta > MIN_AMPLITUDE_RISE &&
                zcrValue < ZCR_TRESHOLD)
            {
                if (speechStartTime < 0) speechStartTime = Time.time;
                else if (Time.time - speechStartTime >= minSpeechDuration)
                    IsUserSpeaking = true;
            }
            else
            {
                speechStartTime = -1f;
            }

            if (IsUserSpeaking)
            {
                if (Amplitude < amplitudeThreshold)
                {
                    ElapsedWaitTime += Time.deltaTime;

                    if (ElapsedWaitTime >= maxWaitTime)
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

            float currentAmplitude = sum / AUDIO_SAMPLE_WINDOW;

            rollingAverage = (rollingAverage * smoothingFactor) + (currentAmplitude * (1 - smoothingFactor));
            Amplitude = Mathf.Clamp01(rollingAverage * AMPLITUDE_MULTIPLIER);
            zcrValue = CalculateZCR(audioSamples);
        }

        private float CalculateZCR(float[] samples)
        {
            int zeroCrossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if ((samples[i - 1] > 0 && samples[i] < 0) ||
                    (samples[i - 1] < 0 && samples[i] > 0))
                {
                    zeroCrossings++;
                }
            }

            return (float)zeroCrossings / samples.Length;
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
