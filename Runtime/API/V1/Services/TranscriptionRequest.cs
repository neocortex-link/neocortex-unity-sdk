using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class TranscriptionRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1/transcribe";
        
        public async Task<string> Send(string projectId, byte[] audio)
        {
            if(string.IsNullOrEmpty(projectId))
            {
                throw new Exception("Project ID is required");
            }
            
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{projectId}",
                payload = audio
            };
            
            ApiResponse response = await Send(request);
            
            return response.transcription;
        }
    }
}
