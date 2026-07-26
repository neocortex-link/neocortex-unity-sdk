using UnityEngine;
using Neocortex.Data;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Neocortex Chat Panel", 0)]
    public class NeocortexChatPanel : ScrollRect
    {
        [SerializeField] private NeocortexMessage writingIndicator;
        [SerializeField] private Font fontOverwrite;

        [Tooltip("Prefab spawned for each chat bubble. Leave empty to use the built-in default (Resources/Prefabs/Message); assign your own NeocortexMessage prefab to restyle every message.")]
        [SerializeField] private NeocortexMessage messageItemPrefab;

        [Tooltip("Show a small avatar with the sender's initial next to each message.")]
        [SerializeField] private bool displayAvatars = true;

        private readonly List<NeocortexMessage> messageItems = new();
        public WritingDirection writingDirection;
        
        [Header("Message Colors")]
        [SerializeField] private Color playerBackground = new Color32(245, 158, 11, 255);
        [SerializeField] private Color playerText = new Color32(31, 31, 31, 255);
        [SerializeField] private Color characterBackground= new Color32(31, 31, 31, 255);
        [SerializeField] private Color characterText= new Color32(245, 158, 11, 255);
        
        protected override void Start()
        {
            base.Start();

            // Only fall back to the built-in bubble when the developer hasn't supplied one,
            // so a prefab assigned in the inspector is never overwritten by the default.
            if (messageItemPrefab == null)
            {
                messageItemPrefab = Resources.Load<NeocortexMessage>("Prefabs/Message");
            }
        }

        /// <summary>
        ///     Adds a bubble. <paramref name="sender"/> is the display name: its initial fills the
        ///     avatar, and it selects that speaker's color. Pass null/empty for no avatar.
        /// </summary>
        public void AddMessage(string sender, string text, bool isUser)
        {
            var isLTR = writingDirection == WritingDirection.LeftToRight;

            var messageItem = Instantiate(messageItemPrefab, content);
            messageItems.Add(messageItem);
            messageItem.SetMessage(sender, text, isUser, isLTR, displayAvatars);
            messageItem.OverwriteFont(fontOverwrite);
            messageItem.SetColor(isUser ? playerBackground : characterBackground, isUser ? playerText : characterText);
            
            writingIndicator.gameObject.SetActive(isUser);
            writingIndicator.transform.SetAsLastSibling();
            writingIndicator.SetMessage(null, "", !isUser, isLTR, false);

            StartCoroutine(ScrollToBottom());
        }

        /// <summary>Adds a bubble with no sender name (so no avatar), the single-character shorthand.</summary>
        public void AddMessage(string text, bool isUser) => AddMessage(null, text, isUser);

        public void ClearMessages()
        {
            foreach (var messageItem in messageItems)
            {
                Destroy(messageItem.gameObject);
            }

            messageItems.Clear();
            writingIndicator.gameObject.SetActive(false);
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            verticalNormalizedPosition = 0;
        }
    }
}
