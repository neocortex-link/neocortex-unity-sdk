using System.Linq;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using Newtonsoft.Json;
using System.Collections;

namespace Neocortex.Samples
{
    public class TextChatSample : MonoBehaviour
    {
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private NeocortexChatPanel chatPanel;
        
        [Space]
        [SerializeField] private GameObject redCube;
        [SerializeField] private GameObject blueCube;
        [SerializeField] private GameObject character;

        private void Start()
        {
            smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        private void OnResponseReceived(ChatResponse response)
        {
            chatPanel.AddMessage(response.message, false);
            ObjectTagData subject = new ObjectTagData();
            
            object[] data = response.data;
            if (data.Length > 0)
            {
                string dataString = JsonConvert.SerializeObject(data);
                var objects = JsonConvert.DeserializeObject<ObjectTagData[]>(dataString);
                
                subject = objects.FirstOrDefault(o => o.isSubject);
            }
            
            string action = response.action;
            if (!string.IsNullOrEmpty(action))
            {
                if (action == "GO_TO_POINT")
                {
                    if (subject.isSubject)
                    {
                        Debug.Log($"GO_TO_POINT {subject.tag}");
                        StartCoroutine(GoToPoint(subject.position));
                    }
                }
            }
        }

        private IEnumerator GoToPoint(Vector3 point)
        {
            float progress = 0;
            while (progress < 1)
            {
                yield return null;
                progress += Time.deltaTime * 0.2f;
                
                character.transform.position = Vector3.Lerp(character.transform.position, point, progress);
            }
        }

        private void Submit(string message)
        {
            chatPanel.AddMessage(message, true);
            smartAgent.TextToText(message);
        }
    }
}
