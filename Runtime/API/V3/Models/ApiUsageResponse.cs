using System;
using Neocortex.Data;

namespace Neocortex.API
{
    [Serializable]
    public class ApiUsageResponse
    {
        public UsageStatus status;
        public int creditsRemaining;
        public PlayerUsage player; // only present when a playerId was passed
        public CharacterUsage character; // only present when a characterId was passed
    }

    [Serializable]
    public class PlayerUsage
    {
        public int interactionsToday;
        public int creditsUsedAllTime;
        public bool overLimit;
    }

    [Serializable]
    public class CharacterUsage
    {
        public int creditsUsedAllTime;
        public bool overLimit;
    }
}
