namespace Neocortex.Data
{
    public class ChatResponse
    {
        public string message;
        public string action;
        public string flowState;
        public Emotions emotion;
        public Interactable[] metadata;
        // Ordered per-emotion chunks; only present when beats were requested (see
        // NeocortexSmartAgent.BeatsMode). Null on older servers or when not requested.
        public Beat[] beats;
    }
}