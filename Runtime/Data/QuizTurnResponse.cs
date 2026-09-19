using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     One turn of a quiz: what the host says now, what the game should put on screen, and what
    ///     to collect next.
    ///
    ///     Order of questions, whether a wrong answer earns another try, when a hint is given and
    ///     when the run ends are all decided on the server from the question set. A game reads this
    ///     and renders it; it never has to decide what happens next.
    /// </summary>
    [Serializable]
    public class QuizTurnResponse
    {
        /// <summary>
        ///     Which run this turn belongs to. Sent on every turn, so a response is always
        ///     self describing however far it travels from the call that made it.
        /// </summary>
        public string sessionId;

        /// <summary>
        ///     What the host says, in order, each with its own emotion and with
        ///     <see cref="QuizLine.role"/> saying what that line is doing. Time the scene to the
        ///     line whose role is <see cref="QuizLineRole.Question"/>, not to the turn.
        /// </summary>
        public QuizLine[] lines;

        /// <summary>
        ///     Game actions the host triggered. <see cref="ChatAction.targetId"/> is a participant id
        ///     when the action is about one player, such as celebrating a winner.
        /// </summary>
        public ChatAction[] actions;

        /// <summary>What to collect from the player next.</summary>
        public QuizExpecting expecting;

        /// <summary>
        ///     How the host read the last thing the player said. Reads as
        ///     <see cref="QuizIntent.Unclear"/> on the opening turn, where nobody has spoken yet.
        /// </summary>
        public QuizIntent intent;

        /// <summary>
        ///     The question now on screen. Carried on the turn it changes and not repeated
        ///     after, so a retry does not resend what the game is already showing. Null means
        ///     "unchanged", not "none".
        /// </summary>
        public QuizQuestion question;

        /// <summary>The question after this one, for preparing its scene early. Null on the last one.</summary>
        public QuizUpcoming upcoming;

        public QuizProgress progress;

        /// <summary>How the last answer was judged, or null on a turn that judged nothing.</summary>
        public QuizResult[] result;

        public QuizLeaderboardEntry[] leaderboard;

        /// <summary>Players the round is still waiting on, for a game with more than one player.</summary>
        public string[] pending;

        /// <summary>True once the host has signed off. Stop sending turns.</summary>
        public bool done;
    }
}
