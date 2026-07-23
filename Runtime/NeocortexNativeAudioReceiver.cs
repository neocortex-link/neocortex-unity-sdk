using System;
using UnityEngine;

namespace Neocortex
{
    // Internal: spawned by NeocortexAudioReceiver on non-WebGL platforms. Add the facade, not this.
    [AddComponentMenu("")]
    public class NeocortexNativeAudioReceiver : AudioReceiver
    {
        private const int FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 64;
        private const int AMPLITUDE_MULTIPLIER = 10;

        private AudioClip audioClip;
        private bool initialized;
        private int startedMicIndex = -1;
        private bool wasPushToTalk;

        public string SelectedMicrophone { get; private set; }
        public bool IsUserSpeaking { get; private set; }

        private void Awake()
        {
            wasPushToTalk = UsePushToTalk;
        }

        public override void StartMicrophone()
        {
            try
            {
                string[] devices = NeocortexMicrophone.devices;
                startedMicIndex = Mathf.Clamp(PlayerPrefs.GetInt(MIC_INDEX_KEY, 0), 0, devices.Length - 1);
                SelectedMicrophone = devices[startedMicIndex];
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
            EmitRecordedClip(audioClip);
        }

        private void Update()
        {
            // Runtime mode switch: leaving continuous listening shuts the hot mic silently (no
            // emit); entering voice-activity mode is started by whoever drives us (the audio
            // widget, ChatUI's re-arm, or a manual StartMicrophone).
            if (UsePushToTalk != wasPushToTalk)
            {
                wasPushToTalk = UsePushToTalk;
                if (UsePushToTalk && initialized)
                {
                    NeocortexMicrophone.End(SelectedMicrophone);
                    initialized = false;
                    IsUserSpeaking = false;
                    ElapsedWaitTime = 0;
                }
            }

            if (!initialized) return;

            // Live mic switching: the selection changed (dropdown, facade API, inspector) while a
            // continuous voice-activity session is listening → hop to the new device now. Waits out
            // an in-progress utterance; push-to-talk picks the change up on the next press anyway.
            if (!UsePushToTalk && !IsUserSpeaking && PlayerPrefs.GetInt(MIC_INDEX_KEY, 0) != startedMicIndex)
            {
                NeocortexMicrophone.End(SelectedMicrophone);
                ElapsedWaitTime = 0;
                StartMicrophone();
                return;
            }

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
