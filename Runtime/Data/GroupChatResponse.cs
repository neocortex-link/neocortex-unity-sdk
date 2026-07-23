using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     A whole group turn: <see cref="messages"/> are the speakers in the order they spoke (one
    ///     entry per speaker; just one for a solo cast), <see cref="sessionId"/> is the shared scene
    ///     session, and <see cref="metadata"/> is the scene's interactables after the turn — echoed
    ///     once for the whole scene, not per speaker.
    /// </summary>
    [Serializable]
    public class GroupChatResponse
    {
        public string sessionId;
        public GroupMessage[] messages;
        public Interactable[] metadata;
    }
}
