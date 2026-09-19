using System;
using UnityEngine;

namespace Neocortex.Data
{
    /// <summary>
    ///     How far through the run the players are. The single source for which question is
    ///     on the table and how many there are, so it is never repeated onto the question
    ///     itself where the two could drift apart.
    /// </summary>
    [Serializable]
    public class QuizProgress
    {
        /// <summary>Which question is on the table, counting from one. Zero before the first.</summary>
        public int asked;

        public int total;

        /// <summary>How many are still to come after this one.</summary>
        public int Remaining => Mathf.Max(0, total - asked);
    }
}
