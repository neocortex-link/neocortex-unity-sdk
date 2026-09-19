using System;
using Neocortex.Data;
using UnityEngine.Networking;

namespace Neocortex.API
{
    [Serializable]
    public class ApiPayload
    {
        public string url;
        public string method = UnityWebRequest.kHttpVerbPOST;
        public object data;
        public ApiResponseType responseType;

        /// <summary>Handler used when <see cref="responseType"/> is Stream, so the body can be read as it arrives.</summary>
        [NonSerialized] public DownloadHandler downloadHandler;

        /// <summary>
        ///     How long to wait for this request before giving up, in seconds. Zero leaves it to
        ///     the platform, which on some of them means forever: a request that never answers
        ///     otherwise leaves an agent busy for the rest of the session.
        /// </summary>
        public int timeoutSeconds;
    }
}
