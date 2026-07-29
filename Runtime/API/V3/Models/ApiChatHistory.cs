using System;
using Neocortex.Data;

namespace Neocortex.API
{
    [Serializable]
    public class ApiChatHistory
    {
        public string sessionId;
        public ChatHistoryEntry[] messages;
        /// <summary>Cursor to the next (older) page; pass it back as `before`. Null at the start of history.</summary>
        public string nextCursor;
    }
}
