namespace Neocortex.Data
{
    /// <summary>
    ///     How the host read what the player said. Not every utterance is an answer, and only
    ///     <see cref="Answer"/> is ever scored.
    /// </summary>
    public enum QuizIntent
    {
        /// <summary>Nothing usable came through. The question stays open and no attempt is spent.</summary>
        Unclear,
        /// <summary>An attempt at the question.</summary>
        Answer,
        /// <summary>Yes, they want another go.</summary>
        Affirm,
        /// <summary>No, move on.</summary>
        Decline,
        /// <summary>They asked to hear the question again.</summary>
        Repeat,
        /// <summary>They asked for the clue the author wrote.</summary>
        Hint,
        /// <summary>They want to stop playing.</summary>
        Stop
    }
}
