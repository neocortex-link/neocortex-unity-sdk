using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Threading;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Neocortex.API
{
    public class WebRequest
    {
        public float Progress { get; private set; }
        public long LastResponseCode { get; private set; }
        public string LastError { get; private set; }
        protected Dictionary<string, string> Headers = new();
        protected CancellationTokenSource CtxSource = new();

        protected async Task<UnityWebRequest> Send(ApiPayload payload)
        {
            UnityWebRequest webRequest;

            LastResponseCode = 0;
            LastError = null;

            if (payload.data == null)
            {
                webRequest = new UnityWebRequest(payload.url, payload.method);
            }
            else if (payload.data is byte[] byteData)
            {
                webRequest = new UnityWebRequest(payload.url, payload.method)
                {
                    uploadHandler = new UploadHandlerRaw(byteData)
                };
            }
            else if (payload.data is List<IMultipartFormSection> formData)
            {
                webRequest = UnityWebRequest.Post(payload.url, formData);
            }
            else
            {
                Debug.LogError("Unsupported payload type");
                return null;
            }
            
            foreach (var header in Headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            switch (payload.responseType)
            {
                case ApiResponseType.Text:
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case ApiResponseType.Audio:
                    webRequest.downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
                    break;
            }

            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                if (CtxSource != null && CtxSource.IsCancellationRequested)
                {
                    webRequest.Abort();
                }
                
                Progress = asyncOperation.progress;
                
                await Task.Yield();
            }
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                return webRequest;
            }
            
            LastResponseCode = webRequest.responseCode;

            if (payload.responseType == ApiResponseType.Audio && webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                string error = System.Text.Encoding.ASCII.GetString(webRequest.downloadHandler.data);
                LastError = error;
                Debug.LogError($"[{webRequest.error}] {error}");
                return null;
            }

            string body = webRequest.downloadHandler.text;
            LastError = string.IsNullOrEmpty(body) ? webRequest.error : body;
            Debug.LogError($"[{webRequest.error}] {body}");
            return null;
        }

        public void Abort()
        {
            CtxSource.Cancel();
        }

        protected byte[] GetBytes(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);

            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
