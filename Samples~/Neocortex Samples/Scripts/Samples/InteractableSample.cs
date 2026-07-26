using UnityEngine;
using Neocortex.Data;
using System.Collections;

namespace Neocortex.Samples
{
    /// <summary>
    ///     The Interactables demo: the character perceives the objects around it. Tag each object
    ///     with a <see cref="NeocortexInteractable"/> (name + properties like colour) — the SDK
    ///     sends them with every message automatically, no code needed.
    ///
    ///     Ask "go to the red cube" and the reply's GO_TO_CUBE action carries the id of the entity
    ///     the character means, so the game knows exactly which one. Because every action carries
    ///     its own target, "go to the blue cube, then the red one" arrives as two actions with two
    ///     different targets and plays out in order.
    /// </summary>
    public class InteractableSample : MonoBehaviour
    {
        [Header("Neocortex Components")]
        [SerializeField] private NeocortexActionQueue actionQueue;
        [SerializeField] private NeocortexSmartAgent agent;

        [Header("Character")]
        [SerializeField] private CharacterController character;
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("How close to the object the character stops.")]
        [SerializeField] private float stopDistance = 0.1f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Talking = Animator.StringToHash("Talking");
        private static readonly int Walking = Animator.StringToHash("Walking");

        private bool isTalking;
        private bool isWalking;

        private void Awake()
        {
            NeocortexSessionManager.CleanSessionID(agent.characterID);

            if (actionQueue == null)
            {
                actionQueue = GetComponent<NeocortexActionQueue>();
            }
        }

        private void Start()
        {
            actionQueue.RegisterAction("GO_TO_LOCATION", GoToLocation);

            // Talking spans the first spoken line to the end of playback.
            agent.OnChatLineStarted.AddListener(_ => isTalking = true);
            agent.OnReplyFinished.AddListener(() => isTalking = false);
        }

        private void Update()
        {
            if (animator == null) return;

            // Thinking = a reply is pending but the character hasn't started speaking yet.
            // Walking is driven by the GO_TO_CUBE handler. Everything false = Idle.
            animator.SetBool(Talking, isTalking);
            animator.SetBool(Walking, isWalking);
            animator.SetBool(Thinking, agent.IsSpeaking && !isTalking);
        }

        /// <summary>Walks to the entity the character targeted with this action.</summary>
        private IEnumerator GoToLocation(ChatAction action)
        {
            NeocortexInteractable target = Find(action.targetId);

            if (target == null)
            {
                yield break;
            }

            // Walk to the LIVE object (so it works even if it has moved) on the ground plane — its
            // height must not count toward "arrived", or a cube resting on the floor is never reached.
            // NOTE: the character's Animator must have Apply Root Motion OFF, or the idle clip pins
            // the root every frame and fights CharacterController.Move.
            isWalking = true;

            while (true)
            {
                Vector3 toTarget = target.transform.position - character.transform.position;
                toTarget.y = 0f;

                if (toTarget.magnitude <= stopDistance) break;

                character.Move(toTarget.normalized * (moveSpeed * Time.deltaTime));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * 8f);

                yield return null;
            }

            isWalking = false;
        }

        private NeocortexInteractable Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (NeocortexInteractable interactable in FindObjectsByType<NeocortexInteractable>(FindObjectsSortMode.None))
            {
                if (interactable.Id == id) return interactable;
            }

            return null;
        }
    }
}
