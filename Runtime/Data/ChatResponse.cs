namespace Neocortex.Data
{
    public class ChatResponse
    {
        public string message;
        public string action;
        public string flowState;
        public Emotions emotion;
        public Interactable[] metadata;
        // Ordered per-emotion chat lines that make up the reply; the server returns them with every
        // character chat, and their text concatenated equals message. May be null/empty on an older server.
        public ChatLine[] lines;
    }
}