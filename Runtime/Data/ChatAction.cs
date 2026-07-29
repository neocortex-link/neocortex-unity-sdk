using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One action the character triggered, in the order it intends to perform it.
    ///     <see cref="targetId"/> is the entity it applies to, or empty when it needs no object.
    /// </summary>
    [Serializable]
    public class ChatAction
    {
        public string name;
        public string targetId;
    }
}
