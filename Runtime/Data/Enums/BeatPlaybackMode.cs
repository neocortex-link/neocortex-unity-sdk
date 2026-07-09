namespace Neocortex.Data
{
    public enum BeatPlaybackMode
    {
        /// <summary>
        ///     Beats are revealed as separate messages in order and the emotion changes with each
        ///     reveal. Audio, if requested, is one clip for the full reply — no extra credit cost
        ///     compared to a normal reply (1 audio credit when audio is used).
        /// </summary>
        TextReveal,

        /// <summary>
        ///     One audio clip is generated PER BEAT, voiced in that beat's emotion, and played
        ///     strictly in order. Emotion changes exactly when each beat's audio starts.
        ///     IMPORTANT: this costs 1 audio credit per beat (up to 4x the cost of a single reply)
        ///     in exchange for lower latency to first audio and tighter emotion sync.
        /// </summary>
        PerBeatAudio,
    }
}
