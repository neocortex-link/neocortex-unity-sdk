using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    public class AudioChatSample : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        [Header("Neocortex Components")]
        [SerializeField] private AudioReceiver audioReceiver;
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexThinkingIndicator thinking;
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexAudioChatInput audioChatInput;

        private void Start()
        {
            NeocortexSessionManager.CleanSessionID(agent.characterID);

            // Deliver replies as chat lines over ONE voice clip (Single Audio) instead of a joint
            // message. The agent plays the clip through the source we hand it here.
            agent.AudioSource = audioSource;
            agent.ChatLinesMode = ChatLinesMode.SingleAudio;

            agent.OnTranscriptionReceived.AddListener(OnTranscriptionReceived);
            agent.OnChatLineStarted.AddListener(OnChatLineStarted);
            agent.OnReplyFinished.AddListener(OnReplyFinished);
            audioReceiver.OnAudioRecorded.AddListener(OnAudioRecorded);
        }

        private void OnAudioRecorded(AudioClip clip)
        {
            agent.AudioToAudio(clip);
            thinking.Display(true);
            audioChatInput.SetChatState(false);
        }

        private void OnTranscriptionReceived(string transcription)
        {
            chatPanel.AddMessage(transcription, true);
        }

        // Each chat line drops in as its own bubble while the reply's voice clip plays.
        private void OnChatLineStarted(ChatLine line)
        {
            thinking.Display(false);
            chatPanel.AddMessage(line.text, false);
        }

        // The whole reply has finished playing — hand the mic back to the player.
        private void OnReplyFinished()
        {
            audioChatInput.SetChatState(true);
            audioReceiver.StartMicrophone();
        }
    }
}
