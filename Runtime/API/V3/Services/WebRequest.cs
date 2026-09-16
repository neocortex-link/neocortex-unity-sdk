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

        /// <summary>How much of an unrecognised error body is worth repeating.</summary>
        private const int MaxErrorLength = 200;

        protected CancellationTokenSource CtxSource = new();

        protected async Task<UnityWebRequest> Send(ApiPayload payload, CancellationToken cancellationToken = default)
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
                case ApiResponseType.Stream:
                    if (payload.downloadHandler == null)
                    {
                        Debug.LogWarning("[Neocortex] Stream response needs a download handler on the payload. Buffering the whole body instead.");
                    }
                    webRequest.downloadHandler = payload.downloadHandler ?? new DownloadHandlerBuffer();
                    break;
            }

            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                if ((CtxSource != null && CtxSource.IsCancellationRequested) || cancellationToken.IsCancellationRequested)
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

            // A caller that cancelled on purpose (the agent moved on, the object went
            // away) is not an error and must not be logged as one.
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            LastResponseCode = webRequest.responseCode;

            if (payload.responseType == ApiResponseType.Audio && webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                byte[] rawData = webRequest.downloadHandler?.data;
                string error = rawData != null && rawData.Length > 0
                    ? System.Text.Encoding.UTF8.GetString(rawData)
                    : webRequest.downloadHandler?.text;

                LastError = string.IsNullOrEmpty(error) ? webRequest.error : error;
                Debug.LogError($"[{webRequest.error}] {DescribeFailure(LastError, LastResponseCode, webRequest.error)}");
                return null;
            }

            string body = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : null;
            LastError = string.IsNullOrEmpty(body) ? webRequest.error : body;
            Debug.LogError($"[{webRequest.error}] {DescribeFailure(LastError, LastResponseCode, webRequest.error)}");
            return null;
        }

        /// <summary>
        ///     Turns a failed response body into something a human can read at a glance.
        /// </summary>
        /// <remarks>
        ///     A request can fail anywhere between here and the API, so the body is not
        ///     necessarily ours: a proxy, a tunnel or a captive portal answers with a whole HTML
        ///     page, and pasting that into the inspector or the Console buries the one line that
        ///     matters. Only a short, non-markup body is worth showing; anything else is reported
        ///     as its status code, which is what actually identifies the failure.
        /// </remarks>
        protected static string DescribeFailure(string body, long statusCode, string transportError)
        {
            string fallback = statusCode > 0 ? $"Request failed ({statusCode})" : "Request failed";
            // A BOM is not whitespace to Trim, and would hide the '<' behind it.
            string text = body?.Trim().Trim('\uFEFF');

            if (string.IsNullOrEmpty(text))
            {
                return string.IsNullOrEmpty(transportError) ? fallback : transportError;
            }

            // An HTML or XML page never came from the API, whatever it says inside.
            if (text[0] == '<')
            {
                return fallback;
            }

            if (text.Length > MaxErrorLength)
            {
                return text.Substring(0, MaxErrorLength).TrimEnd() + "...";
            }

            return text;
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
