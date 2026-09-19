namespace Neocortex.Data
{
    /// <summary>
    ///     What a host line is doing, so a scene can be timed to it rather than to the whole turn.
    ///
    ///     One turn often reacts to the last answer AND asks the next question. A game that swaps
    ///     the scene when the turn arrives puts the next thing on screen while the host is still
    ///     talking about the last one; swapping on <see cref="Question"/> lands it right.
    /// </summary>
    public enum QuizLineRole
    {
        /// <summary>Responding to what the player said. Also the fallback for an unknown value.</summary>
        Reaction,
        /// <summary>Welcoming the player at the top of a run.</summary>
        Greeting,
        /// <summary>The words that actually put the question to the player.</summary>
        Question,
        /// <summary>Signing off at the end of a run.</summary>
        Closing
    }
}
