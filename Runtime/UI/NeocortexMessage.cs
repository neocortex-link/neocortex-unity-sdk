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

        [Tooltip("Background color of the player's own message bubbles.")]
        [SerializeField] private Color userMessageColor = new Color(0f, 0.5f, 0.9f);
        [Tooltip("Background color of the agent's reply bubbles.")]
        [SerializeField] private Color agentMessageColor = new Color(0.8f, 0.8f, 0.8f);
        [Space]
        [Tooltip("Text color inside the player's own message bubbles.")]
        [SerializeField] private Color userTextColor = Color.white;
        [Tooltip("Text color inside the agent's reply bubbles.")]
        [SerializeField] private Color agentTextColor = Color.black;

        protected override void Start()
        {
            base.Start();
            layoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        public void SetMessage(string text, bool isUser, bool isLTR = false)
        {
            if (message)
            {
                message.text = isLTR ? text : text.CorrectRTL();
                message.color = isUser ? userTextColor : agentTextColor;
            }
            background.color = isUser ? userMessageColor : agentMessageColor;

            bool isLeftAligned = isUser ? !isLTR : isLTR;
            layoutGroup.childAlignment = isLeftAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            leftPadding.SetActive(isLeftAligned);
            rightPadding.SetActive(!isLeftAligned);
        }

        public void OverwriteFont(Font font)
        {
            if(font) message.font = font;
        }
    }
}
