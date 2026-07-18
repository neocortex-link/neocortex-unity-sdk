using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    public class TextChatSample : MonoBehaviour
    {
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;

        private void Start()
        {
            // Deliver replies as chat lines (ordered bubbles) instead of one joint message.
            smartAgent.ChatLinesMode = ChatLinesMode.Text;

            smartAgent.OnChatLineStarted.AddListener(OnChatLineStarted);
            smartAgent.OnEmotionChanged.AddListener(emotion => Debug.Log($"[EMOTION] {emotion}"));
            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        // Each chat line drops in as its own bubble.
        private void OnChatLineStarted(ChatLine line)
        {
            thinkingIndicator.Display(false);
            chatPanel.AddMessage(line.text, false);
        }

        private void OnResponseReceived(ChatResponse response)
        {
            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                Debug.Log($"[ACTION] {action}");
            }
        }

        private void Submit(string message)
        {
            chatPanel.AddMessage(message, true);
            smartAgent.TextToText(message);
            thinkingIndicator.Display(true);
        }
    }
}
