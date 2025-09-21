using System.Linq;
using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Collections;

namespace Neocortex.Samples
{
    public class AudioChatSample : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        
        [Header("Neocortex Components")]
        [SerializeField] private AudioReceiver audioReceiver;
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexAudioChatInput audioChatInput;
        
        [Space]
        [SerializeField] private GameObject character;
        
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
            audioChatInput.SetChatState(false);
        }

        private void OnTranscriptionReceived(string transcription)
        {
            chatPanel.AddMessage(transcription, true);
        }

        private void OnChatResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);
            ObjectTagData subject = new ObjectTagData();
            
            object[] data = response.data;
            if (data.Length > 0)
            {
                string dataString = JsonConvert.SerializeObject(data);
                var objects = JsonConvert.DeserializeObject<ObjectTagData[]>(dataString);
                
                subject = objects.FirstOrDefault(o => o.isSubject);
            }
            
            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                if (action == "GO_TO_POINT")
                {
                    if (subject.isSubject)
                    {
                        Debug.Log($"GO_TO_POINT {subject.tag}");
                        StartCoroutine(GoToPoint(subject.position));
                    }
                }
            }
            
            Emotions emotion = response.emotion;
            if (emotion != Emotions.Neutral)
            {
                Debug.Log($"[EMOTION] {emotion.ToString()}");
            }
        }
        
        private void OnAudioResponseReceived(AudioClip audioClip)
        {
            audioSource.clip = audioClip;
            audioSource.Play();

            Invoke(nameof(StartMicrophone), audioClip.length);
            
            audioChatInput.SetChatState(true);
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
