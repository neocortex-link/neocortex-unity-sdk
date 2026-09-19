using System;

namespace Neocortex.Data
{
    [Serializable]
    public class ApiQuestionSetsResponse
    {
        public QuestionSetSummary[] questionSets;
    }

    /// <summary>A question set to pick from, named the way its author named it in the dashboard.</summary>
    [Serializable]
    public class QuestionSetSummary
    {
        public string id;
        public string name;

        /// <summary>How many questions it holds, so a picker can say so without loading the set.</summary>
        public int questionCount;
    }
}
