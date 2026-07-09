namespace Neocortex.Data
{
    /// <summary>
    ///     How a <see cref="Neocortex.NeocortexSmartAgent"/> plays back chat beats.
    /// </summary>
    public enum BeatsMode
    {
        /// <summary>No beats — one reply, exactly as before. Default.</summary>
        Off,

        /// <summary>
        ///     Reveal each beat as its own message in order and change the emotion per beat.
        ///     No extra credit cost.
        /// </summary>
        TextAndEmotion,

        /// <summary>
        ///     Also voice each beat in its own emotion, played progressively in order.
        ///     WARNING: this costs ~1 audio credit PER BEAT (up to 4x a normal reply).
        /// </summary>
        ExpressiveAudio,
    }
}
