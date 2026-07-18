namespace Neocortex.Data
{
    /// <summary>
    ///     How a <see cref="Neocortex.NeocortexSmartAgent"/> delivers a reply. In every mode except
    ///     <see cref="Off"/>, the reply is split into ordered chat lines that drop in one after
    ///     another as separate messages with a short delay; the mode only decides the audio.
    /// </summary>
    public enum ChatLinesMode
    {
        /// <summary>One reply, delivered whole. Default — behaves exactly as before chat lines existed.</summary>
        Off,

        /// <summary>Chat lines drop in as messages with an emotion each. No audio, no extra cost.</summary>
        Text,

        /// <summary>Chat lines drop in while ONE voice clip plays for the whole reply. Costs 1 audio credit.</summary>
        SingleAudio,

        /// <summary>
        ///     Each chat line is voiced separately and plays in order (audio in pieces), with emotion
        ///     synced to its clip. WARNING: costs ~1 audio credit PER line (up to 4x a normal reply).
        /// </summary>
        PerLineAudio,
    }
}
