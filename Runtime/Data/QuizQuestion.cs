using System;

namespace Neocortex.Data
{
    /// <summary>
    ///     The question on screen. Carries everything a game may show and nothing it may not: the
    ///     expected answer, the rubric and the hint text stay on the server until the run is over.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        /// <summary>Generated id. Use <see cref="keyword"/> to decide what to put on screen.</summary>
        public string questionId;

        /// <summary>
        ///     The author's name for the scene this question needs, such as SHOW_RED_APPLE. Empty
        ///     when the author did not set one.
        /// </summary>
        public string keyword;

        /// <summary>
        ///     "free_text", "multiple_choice" or "true_false". Deliberately a string: an unknown
        ///     value must not quietly become a type the game then renders wrongly.
        /// </summary>
        public string type;

        public string questionText;

        /// <summary>The choices to show, empty for an open answer.</summary>
        public QuizOption[] options;

        /// <summary>Whether the author wrote a clue the player can ask for.</summary>
        public bool hasHint;

        /// <summary>True when the player picks from <see cref="options"/> rather than speaking freely.</summary>
        public bool IsChoice => type == "multiple_choice" || type == "true_false";
    }
}
