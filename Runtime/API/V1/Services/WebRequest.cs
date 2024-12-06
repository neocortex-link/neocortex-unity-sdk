using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Neocortex.API
{
    public class WebRequest
    {
        private readonly NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");

        protected async Task<ApiResponse> Send(ApiPayload apiRequest)
        {
            UnityWebRequest webRequest = new UnityWebRequest();
            webRequest.url = apiRequest.url;
            webRequest.method = apiRequest.method;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", settings.apiKey);
            webRequest.uploadHandler = new UploadHandlerRaw(apiRequest.data);
            webRequest.downloadHandler = apiRequest.isAudio
                ? new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG)
                : new DownloadHandlerBuffer();

            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone) await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                if (apiRequest.isAudio)
                {
                    return new ApiResponse()
                    {
                        audio = DownloadHandlerAudioClip.GetContent(webRequest)
                    };
                }
                else
                {
                    return JsonConvert.DeserializeObject<ApiResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                }
            }
            
            Debug.LogError($"[{webRequest.error}] {webRequest.downloadHandler.text}");
            return null;
        }

        protected byte[] GetBytes(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);

            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}