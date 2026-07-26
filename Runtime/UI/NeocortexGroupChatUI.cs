using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     The group counterpart of <see cref="NeocortexChatUI"/>: ONE UI for a whole cast. Connects
    ///     the Neocortex widgets to a <see cref="NeocortexGroupDirector"/> and runs the loop — typed
    ///     and spoken input to the director, every speaker's reply into the shared panel under its
    ///     own name, thinking indicator and mic handoff per turn, history and errors.
    ///
    ///     Put this on the director's GameObject. Do NOT also put a <see cref="NeocortexChatUI"/> on
    ///     the characters: each one would print the same replies again.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Group Chat UI", 0)]
    public class NeocortexGroupChatUI : MonoBehaviour
    {
        [Tooltip("The cast this UI talks to. Auto-resolved from this GameObject when empty.")]
        [SerializeField] private NeocortexGroupDirector director;

        [Tooltip("Name on the player's own messages (the avatar's initial).")]
        [SerializeField] private string playerName = "You";

        [Header("Widgets (Optional)")]
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexTextChatInput textInput;
        [SerializeField] private NeocortexAudioChatInput audioInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexAudioReceiver voiceInput;

        public NeocortexGroupDirector Director { get => director; set => director = value; }
        public NeocortexChatPanel ChatPanel => chatPanel;
        public NeocortexAudioReceiver VoiceInput => voiceInput;
        public string PlayerName { get => playerName; set => playerName = value; }

        private void Awake()
        {
            director ??= GetComponentInChildren<NeocortexGroupDirector>(true);
            chatPanel ??= GetComponentInChildren<NeocortexChatPanel>(true);
            textInput ??= GetComponentInChildren<NeocortexTextChatInput>(true);
            audioInput ??= GetComponentInChildren<NeocortexAudioChatInput>(true);
            thinkingIndicator ??= GetComponentInChildren<NeocortexThinkingIndicator>(true);
            voiceInput ??= GetComponentInChildren<NeocortexAudioReceiver>(true);

            if (director == null)
            {
                Debug.LogError("[Neocortex] Group Chat UI needs a NeocortexGroupDirector — assign one or place it on the director's GameObject.", this);
                enabled = false;
                return;
            }

            // The audio widget needs a receiver before its Start runs; the voice facade IS one.
            if (audioInput != null && audioInput.AudioReceiver == null && voiceInput != null)
            {
                audioInput.AudioReceiver = voiceInput;
            }

            if (textInput != null) textInput.OnSendButtonClicked.AddListener(SubmitText);
            if (voiceInput != null) voiceInput.OnAudioRecorded.AddListener(SubmitAudio);

            director.OnPlayerSpeech.AddListener(HandlePlayerSpeech);
            director.OnSpeaker.AddListener(HandleSpeaker);
            director.OnTurnStarted.AddListener(HandleTurnStarted);
            director.OnTurnFinished.AddListener(HandleTurnFinished);
            director.OnHistoryReceived.AddListener(HandleHistory);
            director.OnRequestFailed.AddListener(HandleRequestFailed);
        }

        private void SubmitText(string message)
        {
            AddMessage(playerName, message, true);
            director.Send(message);
        }

        private void SubmitAudio(AudioClip clip)
        {
            director.SendAudio(clip);
            ShowThinking(true);
            if (audioInput != null) audioInput.SetChatState(false);
        }

        // The player's spoken line, once transcribed.
        private void HandlePlayerSpeech(string transcription)
        {
            AddMessage(playerName, transcription, true);
        }

        private void HandleSpeaker(GroupMessage message)
        {
            ShowThinking(false);
            AddMessage(message.name, JoinLines(message.lines), false);
        }

        private void HandleTurnStarted()
        {
            ShowThinking(true);
            if (audioInput != null) audioInput.SetChatState(false);
        }

        private void HandleTurnFinished()
        {
            ShowThinking(false);
            RearmMicrophone();
        }

        private void HandleHistory(ChatHistoryEntry[] messages)
        {
            foreach (ChatHistoryEntry message in messages)
            {
                bool isUser = message.sender == "USER";
                AddMessage(isUser ? playerName : message.name, message.content, isUser);
            }
        }

        private void HandleRequestFailed(string error)
        {
            ShowThinking(false);
            AddMessage(null, $"[Something went wrong] {error}", false);
            RearmMicrophone(); // a failed turn must not leave the mic locked
        }

        // Helpers

        private void RearmMicrophone()
        {
            if (audioInput != null) audioInput.SetChatState(true);

            // Voice-activity mode listens continuously; push-to-talk re-arms on button press.
            if (voiceInput != null && !voiceInput.UsePushToTalk) voiceInput.StartMicrophone();
        }

        private void AddMessage(string sender, string text, bool isUser)
        {
            if (chatPanel != null && !string.IsNullOrEmpty(text))
            {
                chatPanel.AddMessage(sender, text, isUser);
            }
        }

        private void ShowThinking(bool visible)
        {
            if (thinkingIndicator != null) thinkingIndicator.Display(visible);
        }

        private static string JoinLines(ChatLine[] lines)
        {
            if (lines == null) return "";

            string text = "";
            foreach (ChatLine line in lines) text += line.text + " ";
            return text.Trim();
        }
    }
}
