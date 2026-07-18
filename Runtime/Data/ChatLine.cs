using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One ordered piece of a reply with the emotion it should be delivered in. A reply is split
    ///     into chat lines that drop in one after another; their text concatenated equals the full reply.
    /// </summary>
    [Serializable]
    public class ChatLine
    {
        public string text;
        public Emotions emotion;
    }
}
