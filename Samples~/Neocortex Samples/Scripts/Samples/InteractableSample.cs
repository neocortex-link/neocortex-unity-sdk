using System.Collections;
using System.Linq;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    public class InteractableSample : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Transform character;

        [Header("Neocortex Components")]
        [SerializeField] private AudioReceiver audioReceiver;
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexThinkingIndicator thinking;
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexAudioChatInput audioChatInput;

        private void Start()
        {
            // Deliver replies as chat lines over ONE voice clip (Single Audio) instead of a joint
            // message. The agent plays the clip through the source we hand it here.
            agent.AudioSource = audioSource;
            agent.ChatLinesMode = ChatLinesMode.SingleAudio;

            agent.OnTranscriptionReceived.AddListener(OnTranscriptionReceived);
            agent.OnChatLineStarted.AddListener(OnChatLineStarted);
            agent.OnChatResponseReceived.AddListener(OnChatResponseReceived);
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

        // The reply's action/metadata still arrives once, on the whole response.
        private void OnChatResponseReceived(ChatResponse response)
        {
            Interactable interactable = response.metadata.FirstOrDefault(i => i.isSubject);

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                if (action == "GO_TO_POINT" && interactable != null)
                {
                    Debug.Log($"GO_TO_POINT {interactable.name}");
                    StartCoroutine(GoToPoint(interactable.position));
                }
            }
        }

        // The whole reply has finished playing — hand the mic back to the player.
        private void OnReplyFinished()
        {
            audioChatInput.SetChatState(true);
            audioReceiver.StartMicrophone();
        }

        private IEnumerator GoToPoint(Vector3 point)
        {
            float progress = 0;
            while (progress < 1)
            {
                yield return null;
                progress += Time.deltaTime * 0.2f;

                character.transform.position = Vector3.Lerp(character.transform.position, point, progress);
            }
        }
    }
}
