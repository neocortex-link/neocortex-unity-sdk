using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class WebRequest
    {
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex Settings");

        protected async Task<ApiResponse> Send(ApiRequest apiRequest)
        {
            UnityWebRequest webRequest = new UnityWebRequest();
            webRequest.url = apiRequest.url;
            webRequest.method = apiRequest.method;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", settings.apiKey);
            webRequest.uploadHandler = new UploadHandlerRaw(apiRequest.payload);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone) await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(webRequest.downloadHandler.text);
                return JsonConvert.DeserializeObject<ApiResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
             
            Debug.LogError($"[{webRequest.error}] {webRequest.downloadHandler.text}");
            return null;
        }

        protected byte[] GetBytes(List<Message> messages)
        {
            var payload = new
            {
                messages = messages.ToArray()
            };
            
            string json = JsonConvert.SerializeObject(payload);

            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        
        private string ParseResponse(string response)
        {
            return response
                .Replace("0:", "")
                .Replace("\"\n\"", "")
                .Replace("\n", "")
                .Replace("\\n", "")
                .Replace("\\", "")
                [1..^1];
        }
    }
}