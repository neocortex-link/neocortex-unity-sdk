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
        [SerializeField] private NeocortexMessage writingIndicator;

        private NeocortexMessage messageItemPrefab;
        
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
            messageItem.SetMessage(text, isUser, isLTR);

            writingIndicator.gameObject.SetActive(isUser);
            writingIndicator.transform.SetAsLastSibling();
            writingIndicator.SetMessage("", !isUser, isLTR);
            
            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            verticalNormalizedPosition = 0;
        }
    }
}
