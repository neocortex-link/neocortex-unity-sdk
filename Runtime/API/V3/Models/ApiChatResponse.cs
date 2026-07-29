using System;
using Neocortex.Data;

namespace Neocortex.API
{
    /// <summary>
    ///     The v3 <c>/chat</c> response. One endpoint serves a single character or a whole cast:
    ///     <see cref="messages"/> holds one entry per speaker (exactly one for a solo chat), and
    ///     <see cref="metadata"/> is the scene's interactables echoed once for the whole turn.
    /// </summary>
    [Serializable]
    public class ApiChatResponse
    {
        public string sessionId;
        public GroupMessage[] messages;
        public Interactable[] metadata;
    }
}
