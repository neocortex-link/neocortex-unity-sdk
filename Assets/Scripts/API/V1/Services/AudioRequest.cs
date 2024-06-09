using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class AudioRequest : WebRequest
    {
        private const string BASE_URL = "http://api.localhost:3000/v1/audio"; //"https://api.neocortex.link/v1/audio";
        
        private readonly TranscriptionRequest transcriptionRequest = new TranscriptionRequest();
        private readonly ChatRequest chatRequest = new ChatRequest();

        public async Task<ApiResponse> Send(string id, byte[] audio)
        {
            ApiResponse transcriptionResponse = await transcriptionRequest.Send(id, audio);
            ApiResponse chatResponse = await chatRequest.Send(id, transcriptionResponse.transcription);
            
            var payload = new { text = chatResponse.message };
            
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{id}",
                payload = GetBytes(payload),
                isAudio = true
            };
            
            return await Send(request);
        }
    }
}
