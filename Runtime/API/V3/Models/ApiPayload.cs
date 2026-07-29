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
    }
}
