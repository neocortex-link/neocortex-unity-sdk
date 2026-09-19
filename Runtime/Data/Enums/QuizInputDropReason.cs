namespace Neocortex.Data
{
    /// <summary>
    ///     Why something the player said never reached the host.
    ///
    ///     A quiz turn is a round trip, and a player talks over it: they repeat themselves, they
    ///     answer while the host is still asking, they carry on after the run has ended. Rather
    ///     than dropping any of that in silence, the agent says which of it it let go and why, so
    ///     a game can show "I did not catch that" instead of looking broken.
    /// </summary>
    public enum QuizInputDropReason
    {
        /// <summary>They said something newer before the host was ready. The newer one is sent.</summary>
        Superseded,
        /// <summary>It waited too long to still be an answer to what the host asked.</summary>
        Expired,
        /// <summary>The run has already ended.</summary>
        Finished,
        /// <summary>The quiz has not been started yet.</summary>
        NoSession,
        /// <summary>The turn it would have been sent on failed.</summary>
        Failed,
        /// <summary>The host was busy and this agent is set not to hold anything.</summary>
        Busy
    }
}
