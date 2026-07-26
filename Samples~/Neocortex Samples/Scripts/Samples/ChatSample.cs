using UnityEngine;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Everything the SDK does NOT do for you, which is not much anymore. NeocortexChatUI runs
    ///     the whole conversation loop (input, bubbles, history painting, thinking indicator, mic
    ///     handoff, errors), and history loads via the agent's "Load History On Start" toggle. What
    ///     is left for a sample script is the game-side reaction: driving the character's
    ///     Idle / Thinking / Talking animations and resetting the session.
    ///
    ///     The same script serves the Text Chat and Voice Chat scenes unchanged.
    /// </summary>
    public class ChatSample : MonoBehaviour
    {
        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Talking = Animator.StringToHash("Talking");

        [SerializeField] private NeocortexChatUI chatUI;

        [Tooltip("Optional. Bools 'Thinking' and 'Talking' drive the character; both false = idle.")]
        [SerializeField] private Animator animator;

        [Tooltip("Optional. Clears the conversation and starts a fresh session.")]
        [SerializeField] private Button newSessionButton;

        private NeocortexSmartAgent agent;
        private bool isTalking;
        private RenderTexture faceTexture;

        private void Start()
        {
            agent = chatUI.Agent;

            // Talking = between the first chat line of a reply and the end of its playback.
            agent.OnChatLineStarted.AddListener(_ => isTalking = true);
            agent.OnReplyFinished.AddListener(() => isTalking = false);

            if (newSessionButton != null)
            {
                newSessionButton.onClick.AddListener(StartNewSession);
            }

            // Value overwrites work the same way at runtime, e.g.:
            // agent.ChatLinesMode = ChatLinesMode.SingleAudio; // speak replies (1 credit each)
            // chatUI.VoiceInput.UsePushToTalk = true;          // hold-to-record instead of voice activity
        }

        private void Update()
        {
            if (animator == null || agent == null) return;

            // Thinking = a reply is pending but the character hasn't started speaking yet.
            animator.SetBool(Talking, isTalking);
            animator.SetBool(Thinking, agent.IsSpeaking && !isTalking);
        }

        private void StartNewSession()
        {
            NeocortexSessionManager.CleanSessionID(agent.CharacterID);

            if (chatUI.ChatPanel != null)
            {
                chatUI.ChatPanel.ClearMessages();
            }
        }
    }
}
