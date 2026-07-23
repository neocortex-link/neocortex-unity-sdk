using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One character's turn inside a group reply: the ordered <see cref="ChatLine"/>s it speaks
    ///     (each with its own emotion), the actions it triggered this turn, and its flow state.
    ///     <see cref="name"/> is the character's display name, handy for labeling the line in a
    ///     shared transcript.
    /// </summary>
    [Serializable]
    public class GroupMessage
    {
        public string characterId;
        public string name;
        public ChatLine[] lines;
        public string[] actions;
        public string flowState;
    }
}
