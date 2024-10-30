using System;
using UnityEngine;
using Neocortex.Data;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class AudioRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1/audio";
        
        private readonly TranscriptionRequest transcriptionRequest = new TranscriptionRequest();
        private readonly ChatRequest chatRequest = new ChatRequest();
        
        public async Task<AudioClip> Send(string id, byte[] audio, Action<string> onTranscriptionReceived, Action<ChatResponse> onChatResponseReceived)
        {
            string transcription = await transcriptionRequest.Send(id, audio);
            onTranscriptionReceived?.Invoke(transcription);
            
            ChatResponse chatResponse = await chatRequest.Send(id, transcription);
            onChatResponseReceived?.Invoke(chatResponse);
            
            var payload = new { text = chatResponse.message };
            
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{id}",
                payload = GetBytes(payload),
                isAudio = true
            };
            
            ApiResponse response = await Send(request);

            return response.audio;
        }
    }
}
