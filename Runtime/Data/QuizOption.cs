using System;

namespace Neocortex.Data
{
    /// <summary>One answer a player can pick on a multiple choice or true or false question.</summary>
    [Serializable]
    public class QuizOption
    {
        public string id;
        public string text;
    }
}
