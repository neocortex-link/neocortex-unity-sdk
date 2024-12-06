using System;
using UnityEngine.Networking;

namespace Neocortex.API
{
    [Serializable]
    public class ApiPayload
    {
        public string url;
        public string method = UnityWebRequest.kHttpVerbPOST;
        public byte[] data;
        public bool isAudio;
    }
}
