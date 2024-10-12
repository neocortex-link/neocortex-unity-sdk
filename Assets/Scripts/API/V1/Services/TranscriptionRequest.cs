using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class TranscriptionRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1/transcribe";
        
        public async Task<string> Send(string id, byte[] audio)
        {
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{id}",
                payload = audio
            };
            
            ApiResponse response = await Send(request);
            
            return response.transcription;
        }
    }
}
