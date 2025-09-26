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
            agent.OnTranscriptionReceived.AddListener(OnTranscriptionReceived);
            agent.OnChatResponseReceived.AddListener(OnChatResponseReceived);
            agent.OnAudioResponseReceived.AddListener(OnAudioResponseReceived);
            audioReceiver.OnAudioRecorded.AddListener(OnAudioRecorded);
        }

        private void StartMicrophone()
        {
            audioReceiver.StartMicrophone();
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

        private void OnChatResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);
            Interactable interactable = response.metadata.FirstOrDefault(i => i.isSubject);
            
            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                if (action == "GO_TO_POINT" &&  interactable != null)
                {
                    Debug.Log($"GO_TO_POINT {interactable.name}");
                    StartCoroutine(GoToPoint(interactable.position));
                }
            }
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
