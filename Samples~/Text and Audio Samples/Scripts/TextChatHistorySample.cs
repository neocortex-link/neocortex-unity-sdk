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
            if (smartAgent.GetSessionID() == "")
            {   
                newSessionButton.gameObject.SetActive(false);
            }
            
            newSessionButton.onClick.AddListener(StartNewSession);
            
            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            smartAgent.OnChatHistoryReceived.AddListener(OnChatHistoryReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);
            
            smartAgent.GetChatHistory();
        }

        private void StartNewSession()
        {
            smartAgent.CleanSessionID();
            chatPanel.ClearMessages();
        }

        private void OnChatHistoryReceived(Message[] messages)
        {
            foreach (Message message in messages)
            {
                chatPanel.AddMessage(message.content, message.sender == "USER");
            }
        }

        private void OnResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                Debug.Log($"[ACTION] {action}");
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
