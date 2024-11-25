using UnityEngine;
using Neocortex.Data;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    public class ChatSample : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text outputText;

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent agent;

        private void Start()
        {
            agent.OnChatResponseReceived += OnResponseReceived;
            submitButton.onClick.AddListener(Submit);
        }

        private void OnResponseReceived(ChatResponse response)
        {
            outputText.text += $"Agent: {response.message}\n";

            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                outputText.text += $"[ACTION] {action}\n";
            }

            submitButton.interactable = true;
        }

        private void Submit()
        {
            outputText.text += $"You: {inputField.text}\n";
            agent.Send(inputField.text);

            submitButton.interactable = false;
            inputField.text = "";
        }
    }
}
