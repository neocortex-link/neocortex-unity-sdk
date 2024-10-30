using UnityEngine;
using Neocortex.Data;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    public class AudioTest : MonoBehaviour
    {
        [SerializeField] private Image amplitudeBar;
        [SerializeField] private Text messages;
        [SerializeField] private AudioSource audioSource;

        private NeocortexAudioReceiver audioReceiver;
        private NeocortexSmartAgent agent;

        private void Start()
        {
            agent = GetComponent<NeocortexSmartAgent>();
            agent.OnTranscriptionReceived += OnTranscriptionReceived;
            agent.OnChatResponseReceived += OnChatResponseReceived;
            agent.OnAudioResponseReceived += OnAudioResponseReceived;

            audioReceiver = GetComponent<NeocortexAudioReceiver>();
            audioReceiver.OnAudioRecorded += OnAudioRecorded;
            StartMicrophone();
        }

        private void Update()
        {
            if (audioReceiver)
            {
                amplitudeBar.fillAmount = audioReceiver.Amplitude;
            }
        }

        private void StartMicrophone()
        {
            audioReceiver.StartMicrophone();
        }

        private void OnAudioResponseReceived(AudioClip audioClip)
        {
            audioSource.clip = audioClip;
            audioSource.Play();

            Invoke(nameof(StartMicrophone), audioClip.length);
        }

        private void OnTranscriptionReceived(string transcription)
        {
            messages.text += $"You: {transcription}\n";
        }

        private void OnChatResponseReceived(ChatResponse response)
        {
            messages.text += $"Agent: {response.message}\n";

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                messages.text += $"[ACTION] {action}\n";
            }
        }

        private void OnAudioRecorded(byte[] data)
        {
            amplitudeBar.fillAmount = 0;
            agent.Send(data);
        }
    }
}
