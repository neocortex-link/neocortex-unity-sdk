using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neocortex
{
    public class NeocortexPushToTalkButton : UIBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent OnButtonPressed = new UnityEvent();
        public UnityEvent OnButtonReleased = new UnityEvent();
        
        
        public void OnPointerDown(PointerEventData eventData)
        {
            OnButtonPressed.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnButtonReleased.Invoke();
        }
    }
}
