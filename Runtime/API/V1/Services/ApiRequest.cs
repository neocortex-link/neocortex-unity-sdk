using System;
using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Neocortex.API
{
    public class ApiRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1";
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
        
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
                
                if (settings == null || string.IsNullOrEmpty(settings.apiKey))
                {
                    throw new Exception("API Key is required. Please add it in the Tools > Neocortex > API Key Setup.");
                }

                Headers = new Dictionary<string, string>()
                {
                    { "Content-Type", "application/json" },
                    { "x-api-key", settings.apiKey }
                };
            
                // here transcription request
                if (typeof(TInput) == typeof(AudioClip))
                {
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/transcribe/{projectId}",
                        data = (input as AudioClip).EncodeToWav()
                    };
            
                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    
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
            
                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    
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
                        dataType = ApiResponseDataType.Audio
                    };
                
                    UnityWebRequest request = await Send(payload);
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                    
                    OnAudioResponseReceived?.Invoke(audioClip);
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
