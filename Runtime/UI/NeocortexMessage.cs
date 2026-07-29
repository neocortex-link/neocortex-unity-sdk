using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Neocortex
{
    /// <summary>
    ///     One chat bubble: the text, its alignment (who is speaking, plus the writing direction), an
    ///     optional avatar carrying the sender's initial, and the colors of both.
    ///
    ///     Every widget reference is optional, a message prefab without an avatar simply shows none,
    ///     so prefabs authored before avatars existed keep working untouched.
    /// </summary>
    public class NeocortexMessage : UIBehaviour
    {
        [SerializeField] private Text message;
        [SerializeField] private Image background;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private GameObject leftPadding;
        [SerializeField] private GameObject rightPadding;
        [SerializeField] private GameObject avatar;
        [SerializeField] private Text avatarText;

        private Image avatarBackground;

        protected override void Start()
        {
            base.Start();
            if (!layoutGroup) layoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        public void SetMessage(string sender, string text, bool isUser, bool isLTR = false, bool displayAvatar = true)
        {
            if (message)
            {
                message.text = isLTR ? text : text.CorrectRTL();
            }

            // Start() has not run yet on a bubble instantiated this frame, so resolve on demand.
            if (!layoutGroup) layoutGroup = GetComponent<HorizontalLayoutGroup>();

            bool isLeftAligned = isUser ? !isLTR : isLTR;
            if (layoutGroup) layoutGroup.childAlignment = isLeftAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            if (leftPadding) leftPadding.SetActive(isLeftAligned);
            if (rightPadding) rightPadding.SetActive(!isLeftAligned);

            SetAvatar(sender, isLeftAligned, displayAvatar);
        }

        /// <summary>Tints the bubble and its text; the avatar, when present, follows the same colors.</summary>
        public void SetColor(Color backgroundColor, Color messageColor)
        {
            if (message) message.color = messageColor;
            if (background) background.color = backgroundColor;

            if (avatar && !avatarBackground) avatarBackground = avatar.GetComponent<Image>();
            if (avatarBackground) avatarBackground.color = backgroundColor;
            if (avatarText) avatarText.color = messageColor;
        }

        // The avatar sits on the same side as the speaker's bubble and carries the first letter of
        // their name. An unnamed sender (the writing indicator, or a caller with no name to give)
        // gets no avatar rather than an empty circle.
        private void SetAvatar(string sender, bool isLeftAligned, bool displayAvatar)
        {
            if (!avatar) return;

            string initial = string.IsNullOrWhiteSpace(sender) ? "" : sender.Trim().Substring(0, 1).ToUpper();
            bool show = displayAvatar && initial.Length > 0;

            avatar.SetActive(show);
            if (!show) return;

            avatar.transform.SetSiblingIndex(isLeftAligned ? 3 : 1);
            if (avatarText) avatarText.text = initial;
        }

        public void OverwriteFont(Font font)
        {
            if (!font) return;

            if (message) message.font = font;
            if (avatarText) avatarText.font = font;
        }
    }
}
