using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     The question after the one on screen, so a scene can be built while the host is still
    ///     talking about the current one. Null on the last question.
    /// </summary>
    [Serializable]
    public class QuizUpcoming
    {
        public string questionId;

        /// <summary>The author's scene name for that question, when they set one.</summary>
        public string keyword;
    }
}
