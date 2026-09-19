using System;

namespace Neocortex.Data
{
    /// <summary>One line the host says, with what that line is doing.</summary>
    [Serializable]
    public class QuizLine : ChatLine
    {
        public QuizLineRole role;

        /// <summary>True when this line is the one that actually puts the question to the player.</summary>
        public bool IsQuestion => role == QuizLineRole.Question;
    }
}
