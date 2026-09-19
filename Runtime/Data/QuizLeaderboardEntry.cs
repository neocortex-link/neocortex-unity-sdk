using System;

namespace Neocortex.Data
{
    /// <summary>One player's standing. Players on the same score share a rank.</summary>
    [Serializable]
    public class QuizLeaderboardEntry
    {
        public string participantId;
        public string displayName;
        public int totalScore;
        public int rank;
    }
}
