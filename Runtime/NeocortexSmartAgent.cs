using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;

namespace Neocortex
{
    public class NeocortexSmartAgent : MonoBehaviour
    {
        private ApiRequest apiRequest;
        
        [SerializeField] private string projectId;

        [Space] public UnityEvent<ChatResponse> OnChatResponseReceived;
        [Space] public UnityEvent<AudioClip> OnAudioResponseReceived;
        [Space] public UnityEvent<string> OnTranscriptionReceived;
        [Space] public UnityEvent<string> OnRequestFailed;

        private void Start()
        {
            apiRequest = new ApiRequest();
            apiRequest.OnChatResponseReceived += OnChatResponseReceived.Invoke;
            apiRequest.OnAudioResponseReceived += OnAudioResponseReceived.Invoke;
            apiRequest.OnTranscriptionReceived += OnTranscriptionReceived.Invoke;
            apiRequest.OnRequestFailed += OnRequestFailed.Invoke;
        }
        
        public void SendTextToText(string message)
        {
            apiRequest.Send<string, string>(projectId, message);
        }
        
        public void SendAudioToAudio(AudioClip audio)
        {
            apiRequest.Send<AudioClip, AudioClip>(projectId, audio);
        }
        
        public void SendAudioToText(AudioClip audio)
        {
            apiRequest.Send<AudioClip, string>(projectId, audio);
        }
        
        public void SendTextToAudio(string message)
        {
            apiRequest.Send<string, AudioClip>(projectId, message);
        }
    }
}