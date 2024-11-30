using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    public class AudioChatSample : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        
        [Header("Neocortex Components")]
        [SerializeField] private NeocortexAudioReceiver audioReceiver;
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexThinkingIndicator thinking;
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexAudioChatInput audioChatInput;
        
        private void Start()
        {
            agent.OnTranscriptionReceived += OnTranscriptionReceived;
            agent.OnChatResponseReceived += OnChatResponseReceived;
            agent.OnAudioResponseReceived += OnAudioResponseReceived;
            audioReceiver.OnAudioRecorded += OnAudioRecorded;
        }

        private void StartMicrophone()
        {
            audioReceiver.StartMicrophone();
        }
        
        private void OnAudioRecorded(byte[] data)
        {
            agent.Send(data);
            thinking.Display(true);
            audioChatInput.SetChatState(false);
        }

        private void OnTranscriptionReceived(string transcription)
        {
            chatPanel.AddMessage(transcription, true);
        }

        private void OnChatResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                Debug.Log($"[ACTION] {action}");
            }
        }
        
        private void OnAudioResponseReceived(AudioClip audioClip)
        {
            audioSource.clip = audioClip;
            audioSource.Play();

            Invoke(nameof(StartMicrophone), audioClip.length);
            
            thinking.Display(false);
            audioChatInput.SetChatState(true);
        }
    }
}
