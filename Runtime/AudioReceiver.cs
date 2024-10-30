using UnityEngine;
using UnityEngine.Events;

namespace Neocortex
{
    public class AudioReceiver : MonoBehaviour
    {
        private const int FREQUENCY = 22050;
        private const int AUDIO_SAMPLE_WINDOW = 64;
        private const int AMPLITUDE_MULTIPLIER = 10;
        
        private AudioClip audioClip;
        private float elapsedWaitTime;
        private bool initialized;
        
        public string SelectedMicrophone { get; set; }
        public float Amplitude { get; set; }
        public bool IsRecording { get; set; }
        
        public event UnityAction<byte[]> OnAudioRecorded;
        
        [SerializeField, Range(0, 1)] private float amplitudeTreshold = 0.1f;
        [SerializeField] private float maxWaitTime = 1f;
        
        public void StartMicrophone()
        {
            audioClip = Microphone.Start(SelectedMicrophone, true, 999, FREQUENCY);
            initialized = true;
        }
        
        public void StopMicrophone()
        {
            Microphone.End(SelectedMicrophone);
            IsRecording = false;
        }
        
        private void Update()
        {
            if (!initialized) return;
            
            int position = Microphone.GetPosition(SelectedMicrophone);
            UpdateAmplitude(position, audioClip);
            
            if(!IsRecording && Amplitude > amplitudeTreshold)
            {
                IsRecording = true;
            }
            
            if (IsRecording)
            {
                if (Amplitude < amplitudeTreshold)
                {
                    elapsedWaitTime += Time.deltaTime;
                
                    if(elapsedWaitTime >= maxWaitTime)
                    {
                        IsRecording = false;
                        elapsedWaitTime = 0;
                        AudioClip trimmed = audioClip.Trim();
                        
                        if (trimmed.samples > FREQUENCY)
                        {
                            Microphone.End(SelectedMicrophone);
                            byte[] data = trimmed.EncodeToWav();
                            OnAudioRecorded?.Invoke(data);
                        }
                        else
                        {
                            Debug.Log("Audio too short");
                        }
                    }
                }
                else
                {
                    elapsedWaitTime = 0;
                }
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
    }
}
