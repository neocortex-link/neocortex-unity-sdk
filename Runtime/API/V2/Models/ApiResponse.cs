using System;
using Neocortex.Data;

namespace Neocortex.API
{
    [Serializable]
    public class ApiResponse
    {
        public string sessionId;
        public string message;
        public string action;
        public Emotions emotion;
        public object data;
    }
}
