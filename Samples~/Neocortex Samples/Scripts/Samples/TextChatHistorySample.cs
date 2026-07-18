using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    public class TextChatHistorySample : MonoBehaviour
    {
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private Button newSessionButton;

        private ApiRequest apiRequest;
        
        private void Start()
        {
            if (NeocortexSessionManager.GetSessionID(smartAgent.characterID) == "")
            {   
                newSessionButton.gameObject.SetActive(false);
            }
            
            newSessionButton.onClick.AddListener(StartNewSession);

            // Live replies drop in as chat lines; stored history stays whole (see OnChatHistoryReceived).
            smartAgent.ChatLinesMode = ChatLinesMode.Text;

            smartAgent.OnChatLineStarted.AddListener(OnChatLineStarted);
            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            smartAgent.OnChatHistoryReceived.AddListener(OnChatHistoryReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);

            smartAgent.GetChatHistory();
        }

        private void StartNewSession()
        {
            NeocortexSessionManager.GetSessionID(smartAgent.characterID);
            chatPanel.ClearMessages();
        }

        private void OnChatHistoryReceived(Message[] messages)
        {
            foreach (Message message in messages)
            {
                chatPanel.AddMessage(message.content, message.sender == "USER");
            }
        }

        // Each chat line of a live reply drops in as its own bubble.
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
