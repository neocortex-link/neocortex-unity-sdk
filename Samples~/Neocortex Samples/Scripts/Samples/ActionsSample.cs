using UnityEngine;
using System.Collections;

namespace Neocortex.Samples
{
    /// <summary>
    ///     The Actions demo: pick your character's action keywords (here DANCE and JUMP, as authored
    ///     on its Actions node) and assign each one whatever you want to happen — a handler is a
    ///     coroutine, so anything with an end time works. The <see cref="NeocortexActionQueue"/>
    ///     captures the reply's stacked actions and runs the handlers one after another, in the
    ///     order the character intends ("jump and dance" plays differently than "dance and jump").
    ///
    ///     Also drives the character's Idle / Thinking / Talking states, so this scene is
    ///     self-sufficient: bools "Thinking" and "Talking" plus triggers "Dance" and "Jump" —
    ///     transitions, blending and the return to idle are authored in the Animator Controller.
    /// </summary>
    public class ActionsSample : MonoBehaviour
    {
        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Talking = Animator.StringToHash("Talking");
        private static readonly int Dance = Animator.StringToHash("Dance");
        private static readonly int Jump = Animator.StringToHash("Jump");

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexActionQueue actionQueue;

        [Header("Character")]
        [SerializeField] private Animator animator;

        private bool isTalking;

        private void Start()
        {
            // Talking = between the first chat line of a reply and the end of its playback.
            agent.OnChatLineStarted.AddListener(_ => isTalking = true);
            agent.OnReplyFinished.AddListener(() => isTalking = false);

            // The character's action keywords → whatever should happen in the scene.
            actionQueue.RegisterAction("DANCE", _ => PlayAnimation(Dance, GetClipLength("Dance")));
            actionQueue.RegisterAction("JUMP",  _ => PlayAnimation(Jump, GetClipLength("Jump")));

            actionQueue.OnUnhandledAction += keyword => Debug.LogWarning($"[Neocortex] Agent asked for unknown action '{keyword}'.");
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
        ///     Fires an Animator trigger and holds for the action's duration — the queue starts the
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
