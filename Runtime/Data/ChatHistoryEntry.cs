using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One stored turn of a past conversation, returned by chat history. Unlike a live
    ///     <see cref="ChatLine"/> (a chunk of the current reply), this is a persisted record of a
    ///     whole turn from either side, with who said it and when.
    /// </summary>
    [Serializable]
    public class ChatHistoryEntry
    {
        public string content;
        /// <summary>"USER" (the player) or "ASSISTANT" (a character).</summary>
        public string sender;
        /// <summary>In a group session, which character spoke this ASSISTANT line; null for USER and single-character chats.</summary>
        public string speakerCharacterId;
        /// <summary>Display name of the speaker, resolved server-side; null when there is no speaker (USER lines).</summary>
        public string name;
        /// <summary>Group sessions: who this line was directed at ("Player" or a character name).</summary>
        public string addressedTo;
        /// <summary>The emotion an ASSISTANT line was delivered in; Neutral when not stored (USER lines).</summary>
        public Emotions emotion;
        /// <summary>Stacked actions this ASSISTANT turn triggered; empty for USER lines.</summary>
        public string[] actions;
        public string createdAt;
    }
}
