using System;
using Neocortex.Data;

namespace Neocortex.API
{
    [Serializable]
    public class ApiResponse
    {
        public string sessionId;
        public string response;
        public string action;
        public string flowState;
        public Emotions emotion;
        public Interactable[] metadata;
        // Ordered per-emotion chat lines; the server always returns them. Exposed as ChatResponse.lines.
        public ChatLine[] lines;
    }
}
