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
        public byte[] data;
        public ApiResponseDataType dataType;
    }
}
