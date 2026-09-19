using System;

namespace Neocortex.Data
{
    /// <summary>A player taking part. Only human players are supported for now.</summary>
    [Serializable]
    public class QuizParticipant
    {
        public string id;
        public string displayName;
        public string type = "human";
    }
}
