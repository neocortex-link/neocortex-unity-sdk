using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     Binds a <see cref="NeocortexQuizAgent"/> to the standard chat widgets so a quiz runs with
    ///     no code: host lines land in the panel, the text box sends answers, and typing is locked
    ///     while the host is talking.
    ///
    ///     It only moves values between the agent and the widgets. Which question comes next and
    ///     whether a wrong answer earns another try are decided on the server, and the scene, the
    ///     part that is actually yours, belongs in your own script listening to
    ///     <see cref="NeocortexQuizAgent.OnQuestionChanged"/>.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Quiz UI", 3)]
    public class NeocortexQuizUI : MonoBehaviour
    {
        [Tooltip("The quiz agent to drive. Falls back to one on this GameObject.")]
        public NeocortexQuizAgent agent;

        [Tooltip("Shown as the sender on the player's own answers.")]
        public string playerName = "You";

        [Header("Widgets (auto-resolved from children when left empty)")]
        public NeocortexChatPanel chatPanel;
        public NeocortexTextChatInput textInput;
        public NeocortexThinkingIndicator thinkingIndicator;

        private void Awake()
        {
            agent ??= GetComponent<NeocortexQuizAgent>();
            chatPanel ??= GetComponentInChildren<NeocortexChatPanel>(true);
            textInput ??= GetComponentInChildren<NeocortexTextChatInput>(true);
            thinkingIndicator ??= GetComponentInChildren<NeocortexThinkingIndicator>(true);

            if (agent == null)
            {
                Debug.LogError("[Neocortex] Quiz UI needs a Quiz Agent.", this);
                return;
            }

            if (chatPanel != null)
            {
                agent.OnHostLine.AddListener(line => chatPanel.AddMessage(line.text, false));
            }

            if (textInput != null)
            {
                textInput.OnSendButtonClicked.AddListener(OnPlayerTyped);
            }

            if (thinkingIndicator != null)
            {
                agent.OnBusyChanged.AddListener(thinkingIndicator.Display);
            }

            // Nothing to type into while the host is mid sentence, or once it has signed off.
            agent.OnExpectingChanged.AddListener(SetInputInteractable);
            agent.OnQuizFinished.AddListener(_ => SetInputInteractable(QuizExpecting.Nothing));

            // A player who typed while the host was talking, and whose words were let go, is
            // owed an answer. Silence reads as a broken game.
            agent.OnInputDropped.AddListener(OnInputDropped);
            agent.OnRequestFailed.AddListener(ShowFailure);
        }

        private void OnInputDropped(string text, QuizInputDropReason reason)
        {
            // Superseded means they said something newer and THAT is being sent, so the
            // conversation carries on: saying anything here would only be noise.
            if (reason == QuizInputDropReason.Superseded || chatPanel == null) return;

            chatPanel.AddMessage(DescribeDrop(reason), false);
        }

        private static string DescribeDrop(QuizInputDropReason reason)
        {
            switch (reason)
            {
                case QuizInputDropReason.Expired:
                case QuizInputDropReason.Busy:
                    return "Sorry, I missed that. Say it again?";
                case QuizInputDropReason.Finished:
                    return "That is the end of the quiz.";
                case QuizInputDropReason.NoSession:
                    return "The quiz has not started yet.";
                default:
                    return "Sorry, that did not reach me. Say it again?";
            }
        }

        private void ShowFailure(string error)
        {
            if (chatPanel == null || string.IsNullOrEmpty(error)) return;

            chatPanel.AddMessage(error, false);
        }

        private void OnPlayerTyped(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (chatPanel != null)
            {
                chatPanel.AddMessage($"{playerName}: {message}", true);
            }

            agent.Say(message);
        }

        private void SetInputInteractable(QuizExpecting expecting)
        {
            if (textInput == null) return;

            textInput.gameObject.SetActive(expecting != QuizExpecting.Nothing);
        }
    }
}
