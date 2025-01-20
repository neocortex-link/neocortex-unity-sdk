using UnityEngine;

namespace Neocortex.Samples
{
    public class WebGLCheck : MonoBehaviour
    {
        [SerializeField] private GameObject[] objectsToShow;
        [SerializeField] private GameObject[] objectsToHide;
        
        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var obj in objectsToShow)
            {
                obj.SetActive(true);
            }
            
            foreach (var obj in objectsToHide)
            {
                obj.SetActive(false);
            }
            #endif
        }
    }
}