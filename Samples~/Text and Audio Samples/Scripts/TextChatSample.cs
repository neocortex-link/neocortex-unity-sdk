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
            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        private void OnResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                Debug.Log($"[ACTION] {action}");
            }

            Emotions emotion = response.emotion;
            if (emotion != Emotions.Neutral)
            {
                Debug.Log($"[EMOTION] {emotion.ToString()}");
            }

            thinkingIndicator.Display(false);
        }

        private void Submit(string message)
        {
            chatPanel.AddMessage(message, true);
            smartAgent.TextToText(message);
            thinkingIndicator.Display(true);
        }
    }
}
