using UnityEngine;
using UnityEngine.EventSystems;

namespace Neocortex
{
    [SelectionBase]
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Neocortex/Thinking Indicator", 0)]
    public sealed class NeocortexThinkingIndicator : UIBehaviour
    {
        private readonly static int IS_VISIBLE = Animator.StringToHash("IsVisible");
        
        private Animator animator;

        protected override void Start()
        {
            base.Start();
            animator = GetComponent<Animator>();
        }
        
        public void Display(bool isVisible)
        {
            animator.SetBool(IS_VISIBLE, isVisible);
        }
    }
}
