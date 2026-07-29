namespace Neocortex.Data
{
    /// <summary>
    ///     How a <see cref="Neocortex.NeocortexSmartAgent"/> delivers a reply. Every mode except
    ///     <see cref="Off"/> drops the reply in as ordered chat lines; the mode decides the audio.
    /// </summary>
    public enum ChatLinesMode
    {
        /// <summary>Each line is voiced separately, in order. Costs ~1 audio credit PER line.</summary>
        PerLineAudio,
        
        /// <summary>Chat lines drop in while ONE voice clip plays for the whole reply. Costs 1 audio credit.</summary>
        SingleAudio,

        /// <summary>Chat lines drop in as messages with an emotion each. No audio, no extra cost.</summary>
        Text,

        /// <summary>One reply, delivered whole. Default, behaves exactly as before chat lines existed.</summary>
        Off
    }
}
