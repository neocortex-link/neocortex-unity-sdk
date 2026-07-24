using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     The glue every chat scene used to hand-write: connects a <see cref="NeocortexSmartAgent"/>
    ///     to the Neocortex UI widgets and runs the standard conversation loop — typed and spoken
    ///     input in, transcriptions and chat-line bubbles out, thinking indicator toggled, the mic
    ///     handed back after each reply, stored history painted on load, and request failures
    ///     surfaced in the panel.
    ///
    ///     Every reference except the agent is optional: leave a field empty and it is auto-resolved
    ///     from this GameObject's children; widgets that aren't present are simply skipped, so the
    ///     same component serves text-only, voice-only, and mixed setups. The agent itself stays
    ///     100% UI-free.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Chat UI", 0)]
    public class NeocortexChatUI : MonoBehaviour
    {
        [Tooltip("The character this UI talks to. Auto-resolved from children when empty.")]
        [SerializeField] private NeocortexSmartAgent agent;

        [Header("Widgets (Optional)")]
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexTextChatInput textInput;
        [SerializeField] private NeocortexAudioChatInput audioInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexAudioReceiver voiceInput;

        public NeocortexSmartAgent Agent { get => agent; set => agent = value; }
        public NeocortexChatPanel ChatPanel => chatPanel;
        public NeocortexAudioReceiver VoiceInput => voiceInput;

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
                Debug.LogError("[Neocortex] Chat UI needs a NeocortexSmartAgent — assign one or place it under this GameObject.", this);
                enabled = false;
                return;
            }

            // The audio widget needs a receiver before its Start runs; the voice facade IS one.
            if (audioInput != null && audioInput.AudioReceiver == null && voiceInput != null)
            {
                audioInput.AudioReceiver = voiceInput;
            }

            // Input.
            if (textInput != null)
            {
                textInput.OnSendButtonClicked.AddListener(SubmitText);
            }
            if (voiceInput != null)
            {
                voiceInput.OnAudioRecorded.AddListener(SubmitAudio);
            }

            // Reply.
            agent.OnTranscriptionReceived.AddListener(HandleTranscription);
            agent.OnChatLineStarted.AddListener(HandleChatLine);
            agent.OnChatResponseReceived.AddListener(HandleChatResponse);
            agent.OnComposingNextLine.AddListener(HandleComposingNextLine);
            agent.OnReplyFinished.AddListener(HandleReplyFinished);
            agent.OnChatHistoryReceived.AddListener(HandleHistory);
            agent.OnRequestFailed.AddListener(HandleRequestFailed);
        }

        // ── Input ────────────────────────────────────────────────────────────────────────────────

        private void SubmitText(string message)
        {
            AddMessage(message, true);
            agent.TextToText(message);
            ShowThinking(true);
        }

        private void SubmitAudio(AudioClip clip)
        {
            agent.AudioToAudio(clip);
            ShowThinking(true);

            if (audioInput != null)
            {
                audioInput.SetChatState(false); // lock the mic while the character answers
            }
        }

        // ── Reply ────────────────────────────────────────────────────────────────────────────────

        private void HandleTranscription(string transcription)
        {
            AddMessage(transcription, true);
        }

        private void HandleChatLine(ChatLine line)
        {
            ShowThinking(false);
            AddMessage(line.text, false);
        }

        // Text mode: the character is "typing" the next line during the pacing gap — show the
        // indicator so the pause reads as composing, not dead air. The next line hides it again.
        private void HandleComposingNextLine()
        {
            ShowThinking(true);
        }

        private void HandleChatResponse(ChatResponse response)
        {
            // In chat-lines modes the bubbles arrive via OnChatLineStarted; in Off mode this is the
            // only delivery, so paint the whole reply and hand the mic straight back.
            if (agent.ChatLinesMode == ChatLinesMode.Off)
            {
                ShowThinking(false);
                AddMessage(response.message, false);
                RearmMicrophone();
            }
        }

        private void HandleReplyFinished()
        {
            RearmMicrophone();
        }

        private void HandleHistory(ChatHistoryEntry[] messages)
        {
            if (chatPanel == null) return;

            foreach (ChatHistoryEntry message in messages)
            {
                chatPanel.AddMessage(message.content, message.sender == "USER");
            }
        }

        private void HandleRequestFailed(string error)
        {
            ShowThinking(false);
            AddMessage($"[Something went wrong] {error}", false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────────────────

        private void RearmMicrophone()
        {
            if (audioInput != null)
            {
                audioInput.SetChatState(true);
            }

            // Voice-activity mode listens continuously; push-to-talk re-arms on button press.
            if (voiceInput != null && !voiceInput.UsePushToTalk)
            {
                voiceInput.StartMicrophone();
            }
        }

        private void AddMessage(string text, bool isUser)
        {
            if (chatPanel != null && !string.IsNullOrEmpty(text))
            {
                chatPanel.AddMessage(text, isUser);
            }
        }

        private void ShowThinking(bool visible)
        {
            if (thinkingIndicator != null)
            {
                thinkingIndicator.Display(visible);
            }
        }
    }
}
