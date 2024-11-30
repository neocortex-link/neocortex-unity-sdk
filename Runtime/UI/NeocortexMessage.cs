using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neocortex
{
    public class NeocortexMessage : UIBehaviour
    {
        [SerializeField] private Text message;
        [SerializeField] private Image background;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private GameObject leftPadding;
        [SerializeField] private GameObject rightPadding;
        
        private Color userMessageColor = new Color(0f, 0.5f, 0.9f);
        private Color agentMessageColor = new Color(0.8f, 0.8f, 0.8f);

        protected override void Start()
        {
            base.Start();
            layoutGroup = GetComponent<HorizontalLayoutGroup>();
        }
        
        public void SetMessage(string text, bool isUser)
        {
            message.text = text;
            message.color = isUser ? Color.white : Color.black;
            background.color = isUser ? userMessageColor : agentMessageColor;
            layoutGroup.childAlignment = isUser ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            leftPadding.SetActive(!isUser);
            rightPadding.SetActive(isUser);
        }
    }
}
