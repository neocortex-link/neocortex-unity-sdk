using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Text Chat Input", 0)]
    public class NeocortexTextChatInput : UIBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Font fontOverwrite;
        
        [Space]
        public UnityEvent<string> OnSendButtonClicked = new UnityEvent<string>();
        
        protected override void Start()
        {
            base.Start();
            sendButton.onClick.AddListener(Send);
            inputField.onSubmit.AddListener(Send);
            inputField.textComponent.font = fontOverwrite;
        }
        
        private void Send(string message)
        {
            Send();
        }
        
        private void Send()
        {
            if (inputField.text.Length > 0)
            {
                OnSendButtonClicked.Invoke(inputField.text);
                inputField.text = "";
            }
        }
    }
}
