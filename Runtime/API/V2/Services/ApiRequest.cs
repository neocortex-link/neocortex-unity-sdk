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
        private const string BASE_URL = "https://api.neocortex.link/v2";
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
        private readonly JsonSerializerSettings jsonSerializerSettings = new() { NullValueHandling = NullValueHandling.Ignore };
        
        public event Action<string> OnTranscriptionReceived;
        public event Action<AudioClip> OnAudioResponseReceived;
        public event Action<ChatResponse> OnChatResponseReceived;
        public event Action<string> OnRequestFailed; 
        public event Action<Message[]> OnChatHistoryReceived; 
        
        private string message;

        private void SetHeaders()
        {
            if (settings == null || string.IsNullOrEmpty(settings.apiKey))
            {
                throw new Exception("API Key is required. Please add it in the Tools > Neocortex > API Key Setup.");
            }
            
            Headers = new Dictionary<string, string>()
            {
                { "x-api-key", settings.apiKey }
            };
        }
        
        public async void Send<TInput, TOutput>(string characterId, TInput input)
        {
            try
            {
                if (string.IsNullOrEmpty(characterId))
                {
                    throw new Exception("Project ID is required");
                }

                SetHeaders();
            
                // here transcription request
                if (typeof(TInput) == typeof(AudioClip))
                {
                    List<IMultipartFormSection> form = new List<IMultipartFormSection>
                    {
                        new MultipartFormFileSection("audio", (input as AudioClip).EncodeToWav(), "audio.wav", "audio/wav"),
                        new MultipartFormDataSection("characterId", characterId)
                    };
                    
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/audio/transcribe",
                        data = form,
                        responseType = ApiResponseType.Text
                    };
            
                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    
                    message = response.response;
                    OnTranscriptionReceived?.Invoke(message);
                }
                else
                {
                    message = input as string;
                }
            
                // here chat request
                {
                    var data = new
                    {
                        sessionId = PlayerPrefs.GetString("neocortex-session-id", ""),
                        playerId = SystemInfo.deviceUniqueIdentifier,
                        characterId,
                        message
                    };
                
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/chat",
                        data = GetBytes(data),
                        responseType = ApiResponseType.Text
                    };
            
                    UnityWebRequest request = await Send(payload);
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text, jsonSerializerSettings);

                    PlayerPrefs.SetString("neocortex-session-id", response.sessionId);
                    
                    message = response.response;
                    OnChatResponseReceived?.Invoke(new ChatResponse()
                    {
                        message = message,
                        action = response.action
                    });
                }

                // here audio request
                if (typeof(TOutput) == typeof(AudioClip))
                {
                    var data = new
                    {
                        characterId,
                        message
                    };
                    
                    ApiPayload payload = new ApiPayload()
                    {
                        url = $"{BASE_URL}/audio/generate",
                        data = GetBytes(data),
                        responseType = ApiResponseType.Audio
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

        public async void GetChatHistory(int limit = 10)
        {
            try
            {
                SetHeaders();
                
                var data = new
                {
                    limit,
                    sessionId = PlayerPrefs.GetString("neocortex-session-id", ""),
                };

                ApiPayload payload = new ApiPayload()
                {
                    url = $"{BASE_URL}/chat/session",
                    data = GetBytes(data),
                    responseType = ApiResponseType.Text
                };

                UnityWebRequest request = await Send(payload);
                ApiChatHistory response = JsonConvert.DeserializeObject<ApiChatHistory>(request.downloadHandler.text, jsonSerializerSettings);

                OnChatHistoryReceived?.Invoke(response.messages);
            }
            catch (Exception e)
            {
                OnRequestFailed?.Invoke(e.Message);
                Debug.LogError(e.Message);
            }
        }
    }
}
