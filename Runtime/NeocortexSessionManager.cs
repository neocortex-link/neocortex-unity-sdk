using UnityEngine;

namespace Neocortex
{
    public static class NeocortexSessionManager
    {
        private const string SESSION_ID_KEY = "neocortex-session-id_";

        public static void SetSessionID(string characterID, string sessionId)
        {
            PlayerPrefs.SetString(SESSION_ID_KEY + characterID, sessionId);
        }
        
        public static string GetSessionID(string characterID)
        {
            return PlayerPrefs.GetString(SESSION_ID_KEY + characterID, "");
        }
        
        public static void CleanSessionID(string characterID)
        {
            PlayerPrefs.SetString(SESSION_ID_KEY + characterID, "");
        }
    }
}
