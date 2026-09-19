using System;
using Neocortex.Data;

namespace Neocortex.API
{
    /// <summary>The body of GET /quiz/session: a run as it stands, plus its standings.</summary>
    [Serializable]
    public class ApiQuizSessionResponse
    {
        public QuizPublicState state;
        public QuizLeaderboardEntry[] leaderboard;
        public string startedAt;
        public string finishedAt;
    }
}
