using System;

namespace Neocortex.API
{
    [Serializable]
    public class ApiRequest
    {
        public string url;
        public string method;
        public byte[] payload;
        public bool isAudio;
    }
}
