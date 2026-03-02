namespace Neocortex.Data
{
    public class ChatResponse
    {
        public string message;
        public string action;
        public string flowState;
        public Emotions emotion;
        public Interactable[] metadata;
    }
}