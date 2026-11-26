using System;
using UnityEngine;
using Newtonsoft.Json;

namespace Neocortex.API
{
    /// <summary>
    ///     Tells the developer, never the player, when the workspace has run out of credits:
    ///     which plan fixes it and where to get it. Only in the editor and development builds;
    ///     a release build stays silent, so players never see a billing message.
    /// </summary>
    internal static class OutOfCreditsNotice
    {
        private const string Code = "insufficient_credits";
        private const string DefaultUrl = "https://neocortex.link/dashboard/pricing";
        /// <summary>The dialog shows once per editor session; the warning once per play.</summary>
        private const string DialogShownKey = "Neocortex.OutOfCreditsDialogShown";

        private static bool warned;

        [Serializable]
        private class Upgrade
        {
            public string plan;
            public string url;
        }

        [Serializable]
        private class Body
        {
            public Upgrade upgrade;
        }

        /// <summary>Called for every failed request; does something only for an out-of-credits refusal.</summary>
        public static void Report(string code, string body)
        {
            if (code != Code || warned) return;
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            warned = true;

            Upgrade upgrade = null;
            try
            {
                upgrade = JsonConvert.DeserializeObject<Body>(body)?.upgrade;
            }
            catch
            {
                // An older server sends no upgrade; the plans page still answers.
            }

            string url = string.IsNullOrEmpty(upgrade?.url) ? DefaultUrl : upgrade.url;
            string fix = string.IsNullOrEmpty(upgrade?.plan) ? "a plan with more credits" : $"the {upgrade.plan} plan";
            string message =
                $"Your Neocortex workspace has run out of credits, so your characters stop answering. Upgrade to {fix} at {url}. " +
                "Only you see this, in the editor and development builds.";

            Debug.LogWarning($"[Neocortex] {message}");

#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(DialogShownKey, false)) return;
            UnityEditor.SessionState.SetBool(DialogShownKey, true);
            if (UnityEditor.EditorUtility.DisplayDialog("Neocortex: out of credits", message, "Upgrade", "Later"))
            {
                Application.OpenURL(url);
            }
#endif
        }

        // Domain reload may be off, so the warning is armed again at the start of every play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay() => warned = false;
    }
}
