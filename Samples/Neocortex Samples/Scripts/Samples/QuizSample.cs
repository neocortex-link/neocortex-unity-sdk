using System.Collections.Generic;
using System.Text;
using Neocortex.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    /// <summary>
    ///     The Quiz demo: press Start, type an answer, read what comes back.
    ///
    ///     Everything the host decides is printed as it arrives, so you can watch a whole run
    ///     without any game UI in the way. Which question comes next, whether a wrong answer earns
    ///     another try, when a clue is given and when the run ends are all settled on the server
    ///     from the question set, so none of it is written here.
    /// </summary>
    public class QuizSample : MonoBehaviour
    {
        [Header("Neocortex Components")]
        [SerializeField] private NeocortexQuizAgent agent;

        [Header("Scene")]
        [Tooltip("One entry per question keyword, with the object to show while that question is asked.")]
        [SerializeField] private List<QuizScenePiece> scenePieces = new List<QuizScenePiece>();

        [Header("UI")]
        [SerializeField] private Text log;
        [SerializeField] private InputField answerInput;
        [SerializeField] private Button startButton;

        /// <summary>The author's keyword for a question, and what to put on screen for it.</summary>
        [System.Serializable]
        private class QuizScenePiece
        {
            public string keyword;
            public GameObject target;
        }

        private readonly StringBuilder lines = new StringBuilder();

        private void Start()
        {
            // The scene cue lands BEFORE the host's lines on purpose: the triangle should already
            // be on screen by the time the player hears "what shape is this?". In this text only
            // demo that is why SCENE prints first.
            agent.OnQuestionChanged.AddListener(ShowQuestion);

            // Each line carries the emotion to play it in and what the line is doing, so a
            // real game can drive a face and time its scene off the same event.
            agent.OnHostLine.AddListener(line =>
                Write($"HOST [{line.role}/{line.emotion}]: {line.text}"));
            agent.OnEmotionChanged.AddListener(emotion => Write($"EMOTION: {emotion}"));
            agent.OnResult.AddListener(results =>
            {
                foreach (QuizResult result in results)
                {
                    Write($"RESULT: {result.verdict} ({result.score:+#;-#;0})");
                }
            });
            // Nothing is expected while the host is mid sentence, which is every other line.
            // Only what the host is actually waiting for is worth reading.
            agent.OnExpectingChanged.AddListener(expecting =>
            {
                if (expecting != QuizExpecting.Nothing) Write($"EXPECTING: {expecting}");
            });
            // Start comes back on a failure too: without it a single bad turn bricks the demo
            // with no way on except reloading the scene.
            agent.OnRequestFailed.AddListener(error =>
            {
                Write($"FAILED: {error}");
                SetStartEnabled(true);
            });
            // What the player said that never reached the host, and why. Worth printing in a
            // demo precisely because a real game has to decide what to do about it.
            agent.OnInputDropped.AddListener((text, reason) => Write($"DROPPED [{reason}]: {text}"));
            agent.OnBusyChanged.AddListener(SetInputEnabled);
            agent.OnQuizFinished.AddListener(_ =>
            {
                Write("FINISHED. Press Start for a new run.");
                HideAllShapes();
                SetStartEnabled(true);
            });

            answerInput.onSubmit.AddListener(Send);
            startButton.onClick.AddListener(Begin);

            HideAllShapes();
            SetInputEnabled(false);
            SetStartEnabled(true);
        }

        /// <summary>
        ///     Puts the right thing on screen for the question being asked. The match is on the
        ///     author's keyword, typed in the dashboard, never on the generated question id.
        /// </summary>
        private void ShowQuestion(QuizQuestion question)
        {
            Write($"SCENE: question {agent.Progress.asked}/{agent.Progress.total} wants [{question.keyword}]");

            foreach (QuizScenePiece piece in scenePieces)
            {
                if (piece.target != null)
                {
                    piece.target.SetActive(piece.keyword == question.keyword);
                }
            }
        }

        private void HideAllShapes()
        {
            foreach (QuizScenePiece piece in scenePieces)
            {
                if (piece.target != null) piece.target.SetActive(false);
            }
        }

        /// <summary>Every Begin is a brand new run. Nothing is carried over from the last one.</summary>
        private void Begin()
        {
            lines.Clear();
            HideAllShapes();
            Write("Starting a new run.");

            SetStartEnabled(false);
            agent.Begin();
        }

        private void Send(string text)
        {
            // An empty submit is the documented way to pass on a question, so it is sent rather
            // than swallowed: the host records it unanswered and moves the run along.
            string said = text ?? string.Empty;

            // The host works out whether that was an answer, a yes, a request for the clue or
            // nothing it could make out, so there is nothing to parse here.
            Write(said.Length == 0 ? "YOU: (passed)" : $"YOU: {said}");
            agent.Say(said);

            answerInput.text = string.Empty;
        }

        private void SetInputEnabled(bool busy)
        {
            answerInput.interactable = !busy && !agent.IsFinished;

            if (!busy && !agent.IsFinished)
            {
                answerInput.ActivateInputField();
            }
        }

        private void SetStartEnabled(bool enabled)
        {
            startButton.interactable = enabled;
        }

        private void Write(string line)
        {
            Debug.Log($"[Quiz] {line}");

            if (log == null) return;

            lines.AppendLine(line);
            log.text = lines.ToString();
        }
    }
}
