using System;
using UnityEngine;
using Neocortex.Data;
using System.Collections.Generic;

namespace Neocortex.API
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1";
        
        public event Action<string> OnTranscriptionReceived;
        public event Action<AudioClip> OnAudioResponseReceived;
        public event Action<ChatResponse> OnChatResponseReceived;
        public event Action<string> OnRequestFailed; 
        
        private readonly List<Message> messages = new List<Message>();
        private string lastMessage;
        
        public async void Send<TInput, TOutput>(string projectId, TInput input)
        {
            try
            {
                if (string.IsNullOrEmpty(projectId))
                {
                    throw new Exception("Project ID is required");
                }
            
                // here transcription request
                if (typeof(TInput) == typeof(AudioClip))
                {
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/transcribe/{projectId}",
                        data = (input as AudioClip).EncodeToWav()
                    };
            
                    ApiResponse response = await Send(payload);
                    messages.Add(new Message() { content = response.transcription, role = "user" });
                    OnTranscriptionReceived?.Invoke(response.transcription);
                }
                else
                {
                    messages.Add(new Message() { content = input as string, role = "user" });
                }
            
                // here chat request
                {
                    var data = new { messages = messages.ToArray() };
                
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/chat/{projectId}",
                        data = GetBytes(data)
                    };
            
                    ApiResponse response = await Send(payload);
                    messages.Add(new Message() { content = response.message, role = "assistant" });
                    OnChatResponseReceived?.Invoke(new ChatResponse()
                    {
                        message = response.message,
                        action = response.action
                    });
                }

                // here audio request
                if (typeof(TOutput) == typeof(AudioClip))
                {
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/audio/{projectId}",
                        data = GetBytes(new { text = messages[^1].content }),
                        isAudio = true
                    };
                
                    ApiResponse response = await Send(payload);
                    OnAudioResponseReceived?.Invoke(response.audio);
                }
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
            }
        }
    }
}