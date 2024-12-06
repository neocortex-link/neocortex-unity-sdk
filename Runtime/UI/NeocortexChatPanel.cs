using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("UI/Neocortex/Chat Panel")]
    public class NeocortexChatPanel : ScrollRect
    {
        private NeocortexMessage messageItemPrefab;

        protected override void Start()
        {
            base.Start();
            messageItemPrefab = Resources.Load<NeocortexMessage>("Prefabs/Message");
        }

        public void AddMessage(string text, bool isUser)
        {
            var messageItem = Instantiate(messageItemPrefab, content);
            messageItem.SetMessage(text, isUser);
            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            verticalNormalizedPosition = 0;
        }
    }
}