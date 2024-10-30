using Neocortex.Data;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Neocortex.API
{
    public class ChatRequest : WebRequest
    {
        private const string BASE_URL = "https://api.neocortex.link/v1/chat";
        
        private List<Message> messages = new List<Message>();

        public async Task<ChatResponse> Send(string id, string message)
        {
            messages.Add(new Message() { content = message, role = "user" });
            
            var payload = new { messages = messages.ToArray() };
            
            ApiRequest request = new ApiRequest()
            {
                method = UnityWebRequest.kHttpVerbPOST,
                url = $"{BASE_URL}/{id}",
                payload = GetBytes(payload)
            };
            
            ApiResponse response = await Send(request);
            
            messages.Add(new Message() { content = response.message, role = "assistant" });

            return new ChatResponse()
            {
                message = response.message,
                action = response.action,
            };
        }
    }
}
