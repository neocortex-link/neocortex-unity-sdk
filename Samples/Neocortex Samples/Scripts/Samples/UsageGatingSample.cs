using UnityEngine;
using Neocortex.API;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Text chat gated by the read-only usage endpoint: messages are only sent while the team
    ///     has credits and neither the player nor the character is over a configured cap. Prints the
    ///     account, player and character limits on start, then logs everything to the console.
    /// </summary>
    public class UsageGatingSample : MonoBehaviour
    {
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexSmartAgent smartAgent;

        private NeocortexUsageGate usageGate;

        private async void Start()
        {
            usageGate = new NeocortexUsageGate();

            usageGate.OnUsageUpdated += usage => Debug.Log($"[USAGE] Updated: {Describe(usage)}");
            usageGate.OnLowCredits += usage => Debug.LogWarning($"[USAGE] Credits are running low ({usage.creditsRemaining} left).");
            usageGate.OnCreditsEmpty += _ => Debug.LogWarning("[USAGE] Out of credits: the smart NPC is unavailable.");
            usageGate.OnPlayerOverLimit += _ => Debug.LogWarning("[USAGE] This player has reached their interaction limit for today.");
            usageGate.OnCharacterOverLimit += _ => Debug.LogWarning("[USAGE] This character has reached its usage cap.");
            usageGate.OnRequestFailed += error => Debug.LogWarning($"[USAGE] Usage check failed: {error}");

            smartAgent.chatLinesMode = ChatLinesMode.Text;
            smartAgent.OnChatLineStarted.AddListener(line => Debug.Log($"[CHAT] {smartAgent.gameObject.name}: {line.text} ({line.emotion})"));
            smartAgent.OnRequestFailed.AddListener(error => Debug.LogError($"[CHAT] Request failed: {error}"));

            chatInput.OnSendButtonClicked.AddListener(Submit);

            await PrintLimits();
        }

        // Account tier and credits, then this player's and this character's usage against their caps.
        private async Awaitable PrintLimits()
        {
            ApiAccountResponse account = await usageGate.RefreshAccount();
            if (account != null)
            {
                Debug.Log($"[ACCOUNT] {account.tier} | {account.email} | {account.creditsRemaining} credits | next refresh: {(account.nextRefresh.HasValue ? account.nextRefresh.Value.ToString("u") : "n/a")}");
            }

            ApiUsageResponse usage = await usageGate.RefreshUsage(characterId: smartAgent.characterID);
            Debug.Log(usage != null ? $"[LIMITS] {Describe(usage)}" : "[LIMITS] Usage unavailable.");
        }

        private static string Describe(ApiUsageResponse usage)
        {
            string player = usage.player != null
                ? $"player: {usage.player.interactionsToday} today, {usage.player.creditsUsedAllTime} credits all-time, overLimit={usage.player.overLimit}"
                : "player: not requested";

            string character = usage.character != null
                ? $"character: {usage.character.creditsUsedAllTime} credits all-time, overLimit={usage.character.overLimit}"
                : "character: not requested";

            return $"status={usage.status}, credits={usage.creditsRemaining} | {player} | {character}";
        }

        private async void Submit(string message)
        {
            // Served from cache within NeocortexUsageGate.MinRefreshInterval,
            // so this does not hit the API on every message.
            bool canChat = await usageGate.CanUseService(characterId: smartAgent.characterID);
            if (!canChat)
            {
                Debug.LogWarning("[USAGE] Player limit has been reached, block chat functions if you want.");
                return;
            }

            Debug.Log($"[CHAT] You: {message}");
            smartAgent.TextToText(message);
        }

        public void NewChatSession()
        {
            NeocortexSessionManager.CleanSessionID(smartAgent.characterID);
        }
    }
}
