using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Voice group chat with a living roster. Speak into the mic; an AI director picks which
    ///     characters answer, each speaks aloud in its own voice, and every line drops into ONE
    ///     shared panel labeled by speaker. Characters play Thinking (while the group works out its
    ///     reply) and Talking (while speaking).
    ///
    ///     The roster: a character can wait offstage and WALK IN to join, or LEAVE and walk off — from
    ///     a button. Joining/leaving is not just data: when a character reaches its seat we log a world
    ///     event ("… joined the group") so the NEXT reply greets it; when one leaves we log its exit
    ///     and drop it from the cast, so the others carry on knowing it is gone. All of it rides on the
    ///     director's own AddAgent / RemoveAgent + NeocortexEventLogger — this script is just staging.
    /// </summary>
    public class SimpleGroupChat : MonoBehaviour
    {
        [Serializable]
        private class CastMember
        {
            public NeocortexSmartAgent agent;

            [Tooltip("For walking in/out. Leave empty for a character that never moves.")]
            public CharacterController controller;

            [Tooltip("Where this character stands while in the conversation, facing the user.")]
            public Transform seat;

            [Tooltip("Where this character waits when NOT in the conversation (a corner).")]
            public Transform offstage;

            [Tooltip("Is this character in the conversation when the scene starts?")]
            public bool startsPresent = true;

            [Tooltip("This character's Join/Leave button.")]
            public Toggle toggle;

            [NonSerialized] public Animator animator;
            [NonSerialized] public bool present;
        }

        [Header("Group")]
        [SerializeField] private NeocortexGroupDirector director;
        [SerializeField] private List<CastMember> cast = new();

        [Header("Chat")]
        [Tooltip("The shared transcript. Character replies are printed by each character's own Chat UI; this prints the player's line.")]
        [SerializeField] private NeocortexChatPanel chatPanel;

        [Tooltip("Name shown on the player's own messages (the avatar's initial).")]
        [SerializeField] private string playerName = "You";

        [Tooltip("The mic. Its clip is transcribed and sent to the group.")]
        [SerializeField] private NeocortexAudioReceiver voiceInput;

        [Tooltip("Optional: the record button. Locked while the cast answers.")]
        [SerializeField] private NeocortexAudioChatInput audioInput;

        [Header("Movement")]
        [Tooltip("What seated characters turn to face, usually the Main Camera.")]
        [SerializeField] private Transform faceTarget;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stopDistance = 0.1f;
        [SerializeField] private float turnSpeed = 8f;

        private static readonly int Talking = Animator.StringToHash("Talking");
        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Walking = Animator.StringToHash("Walking");

        private bool turnActive;

        private void Start()
        {
            foreach (CastMember member in cast)
            {
                if (member.agent == null) continue;

                // Each character replies out loud: one voice clip per reply (1 credit). Needs an
                // AudioSource on the character.
                member.animator = member.agent.GetComponentInChildren<Animator>();

                // Snap to the starting spot (no walk on load), then match the cast to it.
                Transform spot = member.startsPresent ? member.seat : member.offstage;
                TeleportTo(member, spot);
                member.present = member.startsPresent;
                if (member.present) director.AddAgent(member.agent);
                else director.RemoveAgent(member.agent);

                CastMember captured = member;
                member.agent.OnReplyFinished.AddListener(() => captured.animator.SetBool(Talking, false));

                if (member.toggle != null)
                {
                    member.toggle.SetIsOnWithoutNotify(member.present); // reflect start state, no callback
                    member.toggle.onValueChanged.AddListener(isOn => OnToggle(captured, isOn));
                }
            }

            if (audioInput != null && audioInput.AudioReceiver == null) audioInput.AudioReceiver = voiceInput;
            voiceInput.OnAudioRecorded.AddListener(director.SendAudio);
            director.OnPlayerSpeech.AddListener(message => chatPanel.AddMessage(playerName, message, true));

            director.OnTurnStarted.AddListener(OnTurnStarted);
            director.OnTurnFinished.AddListener(OnTurnFinished);
            director.OnSpeaker.AddListener(OnSpeaker);
        }

        private void OnSpeaker(GroupMessage message)
        {
            foreach (var agent in director.Agents)
            {
                if (agent.characterID == message.characterId)
                {
                    agent.GetComponent<Animator>().SetBool(Talking, true);
                }
                else
                {
                    agent.GetComponent<Animator>().SetBool(Thinking, false);
                    agent.GetComponent<Animator>().SetBool(Talking, false);
                }
            }
        }

        // Turn flow
        private void OnTurnStarted()
        {
            turnActive = true;

            foreach (var agent in director.Agents)
            {
                agent.GetComponent<Animator>().SetBool(Talking, false);
                agent.GetComponent<Animator>().SetBool(Thinking, true);
            }

            audioInput?.SetChatState(false);       // don't record the characters while they answer
            SetRosterInteractable(false);          // no roster changes mid-turn
        }

        private void OnTurnFinished()
        {
            turnActive = false;

            RearmMic();
            SetRosterInteractable(true);
            
            foreach (var agent in director.Agents)
            {
                agent.GetComponent<Animator>().SetBool(Talking, false);
                agent.GetComponent<Animator>().SetBool(Thinking, false);
            }
        }

        // Join / Leave

        // The toggle flipped: isOn means "should be in the group". Reject mid-turn / mid-walk by
        // snapping the toggle back to the real state (SetIsOnWithoutNotify won't re-fire this).
        private void OnToggle(CastMember member, bool isOn)
        {
            if (isOn == member.present) return;

            if (director.IsBusy)
            {
                Debug.Log("[Neocortex] Wait for the current turn / movement to finish before changing the group.");
                if (member.toggle != null) member.toggle.SetIsOnWithoutNotify(member.present);
                return;
            }

            StartCoroutine(isOn ? JoinRoutine(member) : LeaveRoutine(member));
        }

        private IEnumerator JoinRoutine(CastMember member)
        {
            if (member.toggle != null) member.toggle.interactable = false;

            yield return WalkTo(member, member.seat, faceUserAtEnd: true);

            // In place → part of the conversation now. AddAgent puts this character in the cast; the
            // server diffs the cast against the previous turn and tells the speakers a newcomer
            // arrived.
            member.present = true;
            director.AddAgent(member.agent);
            if (member.toggle != null) member.toggle.interactable = true;

            // Fire an ambient turn so the cast reacts to the arrival right away — greeting the
            // newcomer without the player having to say anything first.
            director.Continue();
        }

        private IEnumerator LeaveRoutine(CastMember member)
        {
            if (member.toggle != null) member.toggle.interactable = false;

            // Drop from the cast BEFORE walking off, so it can't be picked to speak on the way out.
            // The server sees the character missing from the next turn's cast and tells the others it
            // left, so they carry on knowing it is gone — no manual event needed.
            member.present = false;
            director.RemoveAgent(member.agent);

            yield return WalkTo(member, member.offstage, faceUserAtEnd: false);

            if (member.toggle != null) member.toggle.interactable = true;

            // Gone → let the remaining cast react to the departure (ambient turn); the server sees the
            // character missing from the cast and tells the others it left.
            director.Continue();
        }

        // Movement

        private IEnumerator WalkTo(CastMember member, Transform target, bool faceUserAtEnd)
        {
            if (member.controller == null || target == null) yield break;

            member.animator.SetBool(Walking, true);
            Transform body = member.controller.transform;

            // Ground-plane walk. The character's Animator must have Apply Root Motion OFF, or the idle
            // clip fights CharacterController.Move.
            while (true)
            {
                Vector3 toTarget = target.position - body.position;
                toTarget.y = 0f;
                if (toTarget.magnitude <= stopDistance) break;

                member.controller.Move(toTarget.normalized * (moveSpeed * Time.deltaTime));
                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * turnSpeed);
                yield return null;
            }

            member.animator.SetBool(Walking, false);

            if (faceUserAtEnd) yield return FaceUser(body);
        }

        private IEnumerator FaceUser(Transform body)
        {
            if (faceTarget == null) yield break;

            Vector3 toUser = faceTarget.position - body.position;
            toUser.y = 0f;
            if (toUser.sqrMagnitude < 0.001f) yield break;

            Quaternion goal = Quaternion.LookRotation(toUser);
            while (Quaternion.Angle(body.rotation, goal) > 1f)
            {
                body.rotation = Quaternion.Slerp(body.rotation, goal, Time.deltaTime * turnSpeed);
                yield return null;
            }
        }

        private void TeleportTo(CastMember member, Transform spot)
        {
            if (member.controller == null || spot == null) return;

            // A CharacterController resists direct transform writes; disable it for the teleport.
            bool wasEnabled = member.controller.enabled;
            member.controller.enabled = false;
            member.controller.transform.SetPositionAndRotation(spot.position, spot.rotation);
            member.controller.enabled = wasEnabled;

            if (member.startsPresent && faceTarget != null)
            {
                Vector3 toUser = faceTarget.position - member.controller.transform.position;
                toUser.y = 0f;
                if (toUser.sqrMagnitude > 0.001f) member.controller.transform.rotation = Quaternion.LookRotation(toUser);
            }
        }

        // Helpers

        private void RearmMic()
        {
            if (audioInput != null) audioInput.SetChatState(true);
            if (voiceInput != null && !voiceInput.UsePushToTalk) voiceInput.StartMicrophone();
        }

        private void SetRosterInteractable(bool on)
        {
            foreach (CastMember member in cast)
            {
                if (member.toggle != null) member.toggle.interactable = on;
            }
        }
    }
}
