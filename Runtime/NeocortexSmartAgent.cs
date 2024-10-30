using System;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;

namespace Neocortex
{
    public class NeocortexSmartAgent : MonoBehaviour
    {
        [SerializeField] private string id;

        private ChatRequest chatRequest;
        private AudioRequest audioRequest;

        public event Action<ChatResponse> OnChatResponseReceived;
        public event Action<AudioClip> OnAudioResponseReceived;
        public event Action<string> OnTranscriptionReceived;

        private void Start()
        {
            chatRequest = new ChatRequest();
            audioRequest = new AudioRequest();
        }

        public async void Send(string message)
        {
            ChatResponse response = await chatRequest.Send(id, message);
            OnChatResponseReceived?.Invoke(response);
        }

        public async void Send(byte[] audio)
        {
            AudioClip response = await audioRequest.Send(id, audio, OnTranscriptionReceived, OnChatResponseReceived);
            OnAudioResponseReceived?.Invoke(response);
        }
    }
}