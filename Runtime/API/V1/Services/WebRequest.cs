using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Neocortex.API
{
    public class WebRequest
    {
        protected Dictionary<string, string> Headers = new ();

        protected async Task<UnityWebRequest> Send(ApiPayload payload)
        {
            UnityWebRequest webRequest = new UnityWebRequest();
            webRequest.url = payload.url;
            webRequest.method = payload.method;

            foreach (var header in Headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            webRequest.uploadHandler = new UploadHandlerRaw(payload.data);

            switch (payload.dataType)
            {
                case ApiResponseDataType.Text:
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case ApiResponseDataType.Audio:
                    webRequest.downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
                    break;
                case ApiResponseDataType.Texture:
                    webRequest.downloadHandler = new DownloadHandlerTexture(true);
                    break;
            }

            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone) await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                return webRequest;
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
