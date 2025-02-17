using System;
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

        protected async Task<ApiResponse> Send(ApiPayload apiRequest)
        {
            
            UnityWebRequest webRequest = new UnityWebRequest();
            webRequest.url = apiRequest.url;
            webRequest.method = apiRequest.method;

            foreach (var header in Headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            webRequest.uploadHandler = new UploadHandlerRaw(apiRequest.data);

            switch (apiRequest.dataType)
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
                switch (apiRequest.dataType)
                {
                    case ApiResponseDataType.Text:
                        return JsonConvert.DeserializeObject<ApiResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    case ApiResponseDataType.Audio:
                        return new ApiResponse()
                        {
                            data = DownloadHandlerAudioClip.GetContent(webRequest)
                        };
                    case ApiResponseDataType.Texture:
                        return new ApiResponse()
                        {
                            data = DownloadHandlerTexture.GetContent(webRequest)
                        };
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