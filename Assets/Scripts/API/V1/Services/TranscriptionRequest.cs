using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class TranscriptionRequest : WebRequest
    {
        private const string BASE_URL = "http://api.localhost:3000/v1/transcribe"; //"https://api.neocortex.link/v1/transcribe";
        
        public async Task<ApiResponse> Send(string id, byte[] audio)
        {
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{id}",
                payload = audio
            };
            
            return await Send(request);
        }
    }
}
