using UnityEngine;
using System.Collections;

namespace Neocortex.Samples
{
    /// <summary>
    ///     The Actions demo: assign each of the character's action keywords (here DANCE and JUMP) a
    ///     coroutine, and the agent runs them one after another in the order the character intends.
    /// </summary>
    public class ActionsSample : MonoBehaviour
    {
        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Talking = Animator.StringToHash("Talking");
        private static readonly int Dance = Animator.StringToHash("Dance");
        private static readonly int Jump = Animator.StringToHash("Jump");

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent agent;

        [Header("Character")]
        [SerializeField] private Animator animator;

        private bool isTalking;

        private void Start()
        {
            // Talking = between the first chat line of a reply and the end of its playback.
            agent.OnChatLineStarted.AddListener(_ => isTalking = true);
            agent.OnReplyFinished.AddListener(() => isTalking = false);

            // The character's action keywords → whatever should happen in the scene.
            agent.RegisterAction("DANCE", _ => PlayAnimation(Dance, GetClipLength("Dance")));
            agent.RegisterAction("JUMP",  _ => PlayAnimation(Jump, GetClipLength("Jump")));
        }

        private void Update()
        {
            if (animator == null || agent == null) return;

            // Thinking = reply pending but not yet speaking; both false = idle. Dance/Jump ride on
            // top as triggers, so the controller decides how the states blend.
            animator.SetBool(Talking, isTalking);
            animator.SetBool(Thinking, agent.IsSpeaking && !isTalking);
        }

        /// <summary>
        ///     Fires an Animator trigger and holds for the action's duration, the queue starts the
        ///     next action only after this finishes. Transitions are authored in the controller.
        /// </summary>
        private IEnumerator PlayAnimation(int trigger, float duration)
        {
            animator.SetTrigger(trigger);
            yield return new WaitForSeconds(duration);
        }

        /// <summary>The length of an animation clip on the character's controller, by name.</summary>
        private float GetClipLength(string clipName)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName) return clip.length;
            }

            Debug.LogWarning($"[Neocortex] No clip named '{clipName}' on the controller, using 1s.", this);
            return 1f;
        }
    }
}
