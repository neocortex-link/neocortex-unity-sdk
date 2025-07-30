using System;

namespace Neocortex.API
{
    [Serializable]
    public class ApiResponse
    {
        public string sessionId;
        public string response;
        public string action;
    }
}
