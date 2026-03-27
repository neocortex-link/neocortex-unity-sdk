using System;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;

namespace Neocortex
{
    public class NeocortexSmartAgent : MonoBehaviour
    {
        private ApiRequest apiRequest;
        
        public string characterID;

        [Space] public UnityEvent<ChatResponse> OnChatResponseReceived;
        [Space] public UnityEvent<AudioClip> OnAudioResponseReceived;
        [Space] public UnityEvent<string> OnTranscriptionReceived;
        [Space] public UnityEvent<string> OnRequestFailed;
        [Space] public UnityEvent<Message[]> OnChatHistoryReceived;

        private void Awake()
        {
            apiRequest = new ApiRequest();
            apiRequest.OnChatResponseReceived += OnChatResponseReceived.Invoke;
            apiRequest.OnAudioResponseReceived += OnAudioResponseReceived.Invoke;
            apiRequest.OnTranscriptionReceived += OnTranscriptionReceived.Invoke;
            apiRequest.OnChatHistoryReceived += OnChatHistoryReceived.Invoke;
            apiRequest.OnRequestFailed += OnRequestFailed.Invoke;
        }
        
        public void TextToText(string message)
        {
            apiRequest.Send<string, string>(characterID, message);
        }
        
        public void TextToAudio(string message)
        {
            apiRequest.Send<string, AudioClip>(characterID, message);
        }
        
        public void AudioToText(AudioClip audioClip)
        {
            apiRequest.Send<AudioClip, string>(characterID, audioClip);
        }
        
        public void AudioToAudio(AudioClip audioClip)
        {
            apiRequest.Send<AudioClip, AudioClip>(characterID, audioClip);
        }
        
        public void GetChatHistory(int limit = 10)
        {
            apiRequest.GetChatHistory(characterID, limit);
        }

        [Obsolete("This method is replaced by NeocortexSessionManager.GetSessionID")]
        public string GetSessionID()
        {
            return NeocortexSessionManager.GetSessionID(characterID);
        }
        
        [Obsolete("This method is replaced by NeocortexSessionManager.CleanSessionID")]
        public void CleanSessionID()
        {
            NeocortexSessionManager.CleanSessionID(characterID);
        }
    }
}
