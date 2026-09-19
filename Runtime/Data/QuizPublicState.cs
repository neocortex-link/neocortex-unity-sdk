using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     A run read back as it stands right now, rather than as a turn that just happened.
    ///
    ///     A turn is a one-time message: miss it, and the game does not know what the host is
    ///     waiting for. This is the same truth, available at any time, so a client that lost a
    ///     response (the app was backgrounded, the network dropped after the server had already
    ///     applied the turn) can put itself back in step instead of restarting the quiz.
    /// </summary>
    [Serializable]
    public class QuizPublicState
    {
        /// <summary>"created", "started", "collecting", "ready", "evaluated" or "finished".</summary>
        public string phase;

        public int roundNumber;
        public int totalQuestions;
        public int questionsRemaining;

        public QuizParticipant[] participants;

        /// <summary>The question on the table, or null between questions and once the run ends.</summary>
        public QuizQuestion activeQuestion;

        /// <summary>Who has answered this round, for a game with more than one player.</summary>
        public string[] answeredParticipantIds;

        /// <summary>Who the round is still waiting on.</summary>
        public string[] pendingParticipantIds;

        public QuizLeaderboardEntry[] leaderboard;

        /// <summary>What the game should be collecting, the same value a turn carries.</summary>
        public QuizExpecting expecting;

        public string startedAt;
        public string finishedAt;

        /// <summary>True once the host has signed off.</summary>
        public bool IsFinished => phase == "finished";
    }
}
