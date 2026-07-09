using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One ordered chunk of a chat reply with the emotion it should be delivered in.
    ///     Concatenating the text of all beats equals the full <see cref="ChatResponse.message"/>.
    /// </summary>
    [Serializable]
    public class Beat
    {
        public string text;
        public Emotions emotion;
    }
}
