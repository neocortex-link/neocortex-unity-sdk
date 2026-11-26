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

        /// <summary>
        ///     The API's own machine readable code for the last failure ("quiz_finished",
        ///     "operation_in_progress"), when it sent one. Null for a transport failure and for
        ///     anything that did not come from the API.
        /// </summary>
        public string LastErrorCode { get; private set; }

        /// <summary>The last failure, classified. Null after a success or a cancel.</summary>
        public RequestError LastRequestError { get; private set; }

        /// <summary>The last request was cancelled on purpose, so its missing response is not a failure.</summary>
        public bool WasCancelled { get; private set; }

        /// <summary>
        ///     How many times a request that failed with a retryable error is sent again. Applies to
        ///     lookups, history and audio only; a chat or quiz turn is never resent on its own.
        /// </summary>
        public int RetryAttempts = 1;

        /// <summary>Seconds between two attempts.</summary>
        public float RetryDelaySeconds = 1.5f;

        protected Dictionary<string, string> Headers = new();

        /// <summary>How much of an unrecognised error body is worth repeating.</summary>
        private const int MaxErrorLength = 200;

        /// <summary>
        ///     How long past its own timeout a request is given before this side gives up on it.
        ///     UnityWebRequest normally reports the timeout itself; this only covers an operation
        ///     that never completes at all.
        /// </summary>
        private const float TimeoutGraceSeconds = 5f;

        /// <summary>
        ///     Cancels everything this instance has in flight. Replaced rather than reused on
        ///     <see cref="Abort"/>, because a cancelled source stays cancelled: sharing one across
        ///     the life of the object meant a single Abort poisoned every request after it.
        /// </summary>
        protected CancellationTokenSource CtxSource = new();

        protected async Task<UnityWebRequest> Send(ApiPayload payload, CancellationToken cancellationToken = default)
        {
            UnityWebRequest webRequest;

            LastResponseCode = 0;
            LastError = null;
            LastErrorCode = null;
            LastRequestError = null;

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

            if (payload.timeoutSeconds > 0)
            {
                webRequest.timeout = payload.timeoutSeconds;
            }

            // One token for this call: the caller's own cancellation and this instance's Abort,
            // with neither outliving the request nor silencing the next one.
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(CtxSource.Token, cancellationToken);

            WasCancelled = false;
            AsyncOperation asyncOperation = webRequest.SendWebRequest();

            // The timeout above is the transport's; this one is ours. An operation that never
            // reports done (a suspended app, a socket that answers nothing at all) would
            // otherwise spin this loop for the rest of the session.
            float deadline = payload.timeoutSeconds > 0
                ? Time.realtimeSinceStartup + payload.timeoutSeconds + TimeoutGraceSeconds
                : float.MaxValue;
            bool timedOut = false;

            while (!asyncOperation.isDone)
            {
                if (linked.IsCancellationRequested)
                {
                    webRequest.Abort();
                }
                else if (Time.realtimeSinceStartup > deadline)
                {
                    timedOut = true;
                    webRequest.Abort();
                }

                Progress = asyncOperation.progress;

                await Task.Yield();
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Handed over still open, because the body is read from it. The caller disposes.
                return webRequest;
            }

            // A caller that cancelled on purpose (the agent moved on, the object went
            // away, Abort was called) is not an error and must not be logged as one.
            if (linked.IsCancellationRequested && !timedOut)
            {
                WasCancelled = true;
                webRequest.Dispose();
                return null;
            }

            LastResponseCode = webRequest.responseCode;
            string transportError = webRequest.error;

            if (timedOut)
            {
                LastError = $"The request timed out after {payload.timeoutSeconds}s.";
                LastRequestError = new RequestError { message = LastError, timedOut = true, url = payload.url };
                Debug.LogError($"[Neocortex] {LastError} ({payload.url})");
                webRequest.Dispose();
                return null;
            }

            if (payload.responseType == ApiResponseType.Audio && webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                byte[] rawData = webRequest.downloadHandler?.data;
                string error = rawData != null && rawData.Length > 0
                    ? System.Text.Encoding.UTF8.GetString(rawData)
                    : webRequest.downloadHandler?.text;

                LastError = string.IsNullOrEmpty(error) ? transportError : error;
            }
            else
            {
                string body = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : null;
                LastError = string.IsNullOrEmpty(body) ? transportError : body;
            }

            ApiErrorResponse apiError = ReadError(LastError);
            LastErrorCode = apiError?.code;
            LastRequestError = new RequestError
            {
                status = LastResponseCode,
                code = LastErrorCode,
                message = !string.IsNullOrEmpty(apiError?.error) ? apiError.error : DescribeFailure(LastError, LastResponseCode, transportError),
                url = payload.url
            };
            OutOfCreditsNotice.Report(LastErrorCode, LastError);
            if (payload.handledCodes == null || System.Array.IndexOf(payload.handledCodes, LastErrorCode) < 0)
            {
                Debug.LogError($"[{transportError}] {DescribeFailure(LastError, LastResponseCode, transportError)}");
            }
            webRequest.Dispose();
            return null;
        }

        /// <summary>
        ///     Sends a request, and sends it again after <see cref="RetryDelaySeconds"/> while it fails
        ///     with a retryable error, up to <see cref="RetryAttempts"/> more times.
        /// </summary>
        protected async Task<UnityWebRequest> SendWithRetry(ApiPayload payload)
        {
            UnityWebRequest request = await Send(payload);

            for (int attempt = 0; request == null && attempt < RetryAttempts; attempt++)
            {
                if (WasCancelled || LastRequestError == null || !LastRequestError.IsRetryable()) break;

                // Captured before the wait, since Abort replaces the source.
                CancellationToken token = CtxSource.Token;
                float resumeAt = Time.realtimeSinceStartup + RetryDelaySeconds;
                while (Time.realtimeSinceStartup < resumeAt && !token.IsCancellationRequested)
                {
                    await Task.Yield();
                }

                if (token.IsCancellationRequested)
                {
                    WasCancelled = true;
                    LastRequestError = null;
                    return null;
                }

                request = await Send(payload);
            }

            return request;
        }

        /// <summary>
        ///     Reads the API's error body, so a caller can branch on its code rather than on the
        ///     wording of a sentence. Null when the body is not the API's.
        /// </summary>
        private static ApiErrorResponse ReadError(string body)
        {
            string text = body?.TrimStart();
            if (string.IsNullOrEmpty(text) || text[0] != '{') return null;

            try
            {
                return JsonConvert.DeserializeObject<ApiErrorResponse>(text);
            }
            catch
            {
                // Not our shape, so whatever answered was not the API.
                return null;
            }
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

        /// <summary>Cancels everything this instance has in flight, and leaves it usable after.</summary>
        public void Abort()
        {
            CancellationTokenSource aborted = CtxSource;

            // Swapped BEFORE cancelling, so anything started during the cancel gets the fresh
            // source rather than an already-cancelled one. The old source is deliberately not
            // disposed: requests still unwinding hold tokens linked to it.
            CtxSource = new CancellationTokenSource();
            aborted.Cancel();
        }

        protected byte[] GetBytes(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);

            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
