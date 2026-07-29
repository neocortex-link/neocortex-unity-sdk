using System;

namespace Neocortex.API
{
    [Serializable]
    public class ApiAccountResponse
    {
        public string tier;
        public string email;
        public int creditsRemaining;
        public DateTime? nextRefresh;
    }
}
