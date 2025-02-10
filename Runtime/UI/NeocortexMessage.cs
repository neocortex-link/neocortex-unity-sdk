using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        public void SetMessage(string text, bool isUser, bool isLTR = false)
        {
            message.text = isLTR ? text : text.CorrectRTL();
            message.color = isUser ? Color.white : Color.black;
            background.color = isUser ? userMessageColor : agentMessageColor;

            bool isLeftAligned = isUser ? !isLTR : isLTR;
            layoutGroup.childAlignment = isLeftAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            leftPadding.SetActive(isLeftAligned);
            rightPadding.SetActive(!isLeftAligned);
        }
    }
}
