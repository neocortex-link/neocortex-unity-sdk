namespace Neocortex.Data
{
    /// <summary>
    ///     What the quiz is waiting for. Open the microphone, or show the answer controls, while
    ///     this is anything but <see cref="Nothing"/>.
    /// </summary>
    public enum QuizExpecting
    {
        /// <summary>The host is talking, or the quiz is over. Collect nothing.</summary>
        Nothing,
        /// <summary>An attempt at the question on screen.</summary>
        Answer,
        /// <summary>A yes or a no, usually to an offer of another try.</summary>
        Confirmation
    }
}
