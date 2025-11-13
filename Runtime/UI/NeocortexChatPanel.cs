using UnityEngine;
using Neocortex.Data;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Neocortex
{
    [SelectionBase]
    [AddComponentMenu("Neocortex/Chat Panel", 0)]
    public class NeocortexChatPanel : ScrollRect
    {
        [SerializeField] private NeocortexMessage writingIndicator;

        private NeocortexMessage messageItemPrefab;
        private List<NeocortexMessage> messageItems = new();
        
        public WritingDirection writingDirection;

        protected override void Start()
        {
            base.Start();
            messageItemPrefab = Resources.Load<NeocortexMessage>("Prefabs/Message");
        }

        public void AddMessage(string text, bool isUser)
        {
            var isLTR = writingDirection == WritingDirection.LeftToRight;
            
            var messageItem = Instantiate(messageItemPrefab, content);
            messageItems.Add(messageItem);
            messageItem.SetMessage(text, isUser, isLTR);

            writingIndicator.gameObject.SetActive(isUser);
            writingIndicator.transform.SetAsLastSibling();
            writingIndicator.SetMessage("", !isUser, isLTR);
            
            StartCoroutine(ScrollToBottom());
        }
        
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
