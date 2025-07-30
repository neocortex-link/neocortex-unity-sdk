using System;
using Neocortex.Data;

namespace Neocortex.API
{
    [Serializable]
    public class ApiChatHistory
    {
        public string sessionId;
        public Message[] messages;
    }
}