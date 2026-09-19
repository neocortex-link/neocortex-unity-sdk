using System;

namespace Neocortex.Data
{
    /// <summary>How one player's attempt was judged, and what it was worth.</summary>
    [Serializable]
    public class QuizResult
    {
        public string participantId;
        public QuizVerdict verdict;
        public int score;
    }
}
