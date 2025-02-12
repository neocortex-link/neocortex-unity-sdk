using UnityEngine;
using Neocortex.Data;
using UnityEngine.UI;
using System.Collections;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Chat Panel", 0)]
    public class NeocortexChatPanel : ScrollRect
    {
        public WritingDirection writingDirection;
        private NeocortexMessage messageItemPrefab;

        protected override void Start()
        {
            base.Start();
            messageItemPrefab = Resources.Load<NeocortexMessage>("Prefabs/Message");
        }

        public void AddMessage(string text, bool isUser)
        {
            var messageItem = Instantiate(messageItemPrefab, content);
            messageItem.SetMessage(text, isUser, writingDirection == WritingDirection.LeftToRight);
            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            verticalNormalizedPosition = 0;
        }
    }
}
