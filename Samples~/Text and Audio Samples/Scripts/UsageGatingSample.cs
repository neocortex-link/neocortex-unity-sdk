using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Text chat gated by the read-only usage endpoint: messages are only sent while the team
    ///     has credits and neither the player nor the character is over a configured cap.
    /// </summary>
    public class UsageGatingSample : MonoBehaviour
    {
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;

        private NeocortexUsageGate usageGate;

        private void Start()
        {
            usageGate = new NeocortexUsageGate();

            usageGate.OnLowCredits += usage => Debug.LogWarning($"[USAGE] Credits are running low ({usage.creditsRemaining} left).");
            usageGate.OnCreditsEmpty += _ => chatPanel.AddMessage("The smart NPC is unavailable right now.", false);
            usageGate.OnPlayerOverLimit += _ => chatPanel.AddMessage("You have reached your interaction limit for today.", false);
            usageGate.OnCharacterOverLimit += _ => Debug.LogWarning("[USAGE] This character has reached its usage cap.");
            usageGate.OnRequestFailed += error => Debug.LogWarning($"[USAGE] Usage check failed: {error}");

            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        private void OnResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);
            thinkingIndicator.Display(false);
        }

        private async void Submit(string message)
        {
            // Served from cache within NeocortexUsageGate.MinRefreshInterval,
            // so this does not hit the API on every message.
            bool canChat = await usageGate.CanUseSmartNPC(characterId: smartAgent.characterID);
            if (!canChat)
            {
                chatPanel.AddMessage("Smart NPC features are currently disabled.", false);
                return;
            }

            chatPanel.AddMessage(message, true);
            smartAgent.TextToText(message);
            thinkingIndicator.Display(true);
        }
    }
}
