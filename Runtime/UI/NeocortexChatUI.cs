using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     Connects the Neocortex UI widgets to ONE character and runs the standard loop: input in,
    ///     bubbles out, thinking indicator, mic handoff, history and errors. Widget references are
    ///     optional and auto-resolved from this GameObject's children.
    ///
    ///     For a multi-character scene use <see cref="NeocortexGroupChatUI"/> with a
    ///     <see cref="NeocortexGroupDirector"/> instead, one UI for the whole cast.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Chat UI", 0)]
    public class NeocortexChatUI : MonoBehaviour
    {
        [Tooltip("The character this UI talks to. Auto-resolved from children when empty.")]
        [SerializeField] private NeocortexSmartAgent agent;

        [Tooltip("Name on the player's own messages (the avatar's initial).")]
        [SerializeField] private string playerName = "You";

        [Header("Widgets (Optional)")]
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexTextChatInput textInput;
        [SerializeField] private NeocortexAudioChatInput audioInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexAudioReceiver voiceInput;

        public NeocortexSmartAgent Agent { get => agent; set => agent = value; }
        public NeocortexChatPanel ChatPanel => chatPanel;
        public NeocortexAudioReceiver VoiceInput => voiceInput;
        public string PlayerName { get => playerName; set => playerName = value; }

        // Resolved from each reply so chat lines, which carry only text, can still be attributed.
        private string characterName;

        private void Awake()
        {
            agent ??= GetComponentInChildren<NeocortexSmartAgent>(true);
            chatPanel ??= GetComponentInChildren<NeocortexChatPanel>(true);
            textInput ??= GetComponentInChildren<NeocortexTextChatInput>(true);
            audioInput ??= GetComponentInChildren<NeocortexAudioChatInput>(true);
            thinkingIndicator ??= GetComponentInChildren<NeocortexThinkingIndicator>(true);
            voiceInput ??= GetComponentInChildren<NeocortexAudioReceiver>(true);

            if (agent == null)
            {
                Debug.LogError("[Neocortex] Chat UI needs a NeocortexSmartAgent, assign one or place it under this GameObject.", this);
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

            agent.OnTranscriptionReceived.AddListener(HandleTranscription);
            agent.OnChatLineStarted.AddListener(HandleChatLine);
            agent.OnChatResponseReceived.AddListener(HandleChatResponse);
            agent.OnComposingNextLine.AddListener(HandleComposingNextLine);
            agent.OnReplyFinished.AddListener(HandleReplyFinished);
            agent.OnChatHistoryReceived.AddListener(HandleHistory);
            agent.OnRequestFailed.AddListener(HandleRequestFailed);
        }

        private void SubmitText(string message)
        {
            AddMessage(playerName, message, true);
            agent.TextToText(message);
            ShowThinking(true);
        }

        private void SubmitAudio(AudioClip clip)
        {
            agent.AudioToAudio(clip);
            ShowThinking(true);
            if (audioInput != null) audioInput.SetChatState(false);
        }

        private void HandleTranscription(string transcription)
        {
            AddMessage(playerName, transcription, true);
        }

        private void HandleChatLine(ChatLine line)
        {
            ShowThinking(false);
            AddMessage(CharacterName, line.text, false);
        }

        // Text mode: the character is "typing" the next line during the pacing gap.
        private void HandleComposingNextLine()
        {
            ShowThinking(true);
        }

        private void HandleChatResponse(ChatResponse response)
        {
            // Raised before the reply's lines drop, so the name learned here labels them too.
            if (!string.IsNullOrEmpty(response.name)) characterName = response.name;

            // Off mode delivers the whole reply here instead of line by line.
            if (agent.ChatLinesMode == ChatLinesMode.Off)
            {
                ShowThinking(false);
                AddMessage(CharacterName, response.message, false);
                RearmMicrophone();
            }
        }

        private void HandleReplyFinished()
        {
            RearmMicrophone();
        }

        private void HandleHistory(ChatHistoryEntry[] messages)
        {
            foreach (ChatHistoryEntry message in messages)
            {
                bool isUser = message.sender == "USER";
                string sender = isUser ? playerName : (string.IsNullOrEmpty(message.name) ? CharacterName : message.name);
                AddMessage(sender, message.content, isUser);
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

        private string CharacterName =>
            string.IsNullOrEmpty(characterName) ? agent.gameObject.name : characterName;

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
    }
}
