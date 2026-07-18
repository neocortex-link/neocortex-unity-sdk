using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Text chat using chat lines: each reply drops in as ordered messages, one after another,
    ///     with an emotion per line. Pick how the audio is delivered with <see cref="chatLinesMode"/>:
    ///     Text (no audio), SingleAudio (one clip, 1 credit), or PerLineAudio (a clip per line,
    ///     ~1 credit each). Audio modes need an AudioSource assigned on the agent.
    /// </summary>
    public class ChatLinesSample : MonoBehaviour
    {
        [Tooltip("How replies are delivered. Per-Line Audio costs ~1 credit per line; Single Audio costs 1.")]
        [SerializeField] private ChatLinesMode chatLinesMode = ChatLinesMode.Text;

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;

        private void Start()
        {
            agent.ChatLinesMode = chatLinesMode;

            agent.OnChatLineStarted.AddListener(OnChatLineStarted);
            agent.OnEmotionChanged.AddListener(emotion => Debug.Log($"[EMOTION] {emotion}"));
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        // Each chat line drops in here as its own chat bubble.
        private void OnChatLineStarted(ChatLine line)
        {
            thinkingIndicator?.Display(false);
            chatPanel.AddMessage(line.text, false);
        }

        private void Submit(string message)
        {
            chatPanel.AddMessage(message, true);

            // Queued automatically if the character is still speaking; sent when the reply finishes.
            agent.TextToText(message);
            thinkingIndicator?.Display(true);
        }
    }
}
