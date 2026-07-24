namespace Neocortex.Data
{
    public class ChatResponse
    {
        public string message;
        // Name of the first of <see cref="actions"/>, kept for existing integrations that read a single action.
        public string action;
        // All stacked actions the character triggered this turn, each with the entity it targets. Empty when none.
        public ChatAction[] actions;
        public string flowState;
        public Emotions emotion;
        public Interactable[] metadata;
        // Ordered per-emotion chat lines that make up the reply; the server returns them with every
        // character chat, and their text concatenated equals message. May be null/empty on an older server.
        public ChatLine[] lines;
        // The character that produced this reply, and its display name. Useful when one handler
        // serves several agents (e.g. a group scene).
        public string characterId;
        public string name;
    }
}
