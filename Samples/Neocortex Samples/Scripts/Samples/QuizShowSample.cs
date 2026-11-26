using System;
using System.Linq;
using Neocortex.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Neocortex.Samples
{
    /// <summary>
    ///     The character hosts a quiz from one of your question sets, for one to four players at one
    ///     screen. The host's lines and the answers run as bubbles, tapped choices sit under them, and
    ///     "Reply As" chips pick who answers next, with each player's score. The question card,
    ///     Start, Abort and Resync are on the right, with Hint, Pass and Pass Everyone below.
    /// </summary>
    public class QuizShowSample : MonoBehaviour
    {
        private static readonly int Thinking = Animator.StringToHash("Thinking");
        private static readonly int Talking = Animator.StringToHash("Talking");

        private static readonly Color Amber = new(0.9607843f, 0.61960787f, 0.043137256f, 1);
        private static readonly Color Muted = new(1, 1, 1, 0.59f);
        private static readonly Color Good = new(0.36f, 0.79f, 0.56f, 1);
        private static readonly Color Half = new(0.94f, 0.77f, 0.36f, 1);
        private static readonly Color Bad = new(0.93f, 0.42f, 0.42f, 1);
        private static readonly Color ChipFill = new(0.129f, 0.129f, 0.129f, 1);
        private static readonly Color ChipCurrent = new(0.2f, 0.2f, 0.2f, 1);

        /// <summary>One player's "Reply As" chip.</summary>
        [Serializable]
        private class Seat
        {
            public Button button;
            public Image fill;
            public Outline outline;
            public Text name;
            public Text score;
        }

        [SerializeField] private NeocortexQuizAgent quiz;

        [Tooltip("The host's name on its bubbles. The launcher sets it to the picked character.")]
        public string hostName = "Cora";

        [Tooltip("The players, one to four. One person plays every seat.")]
        [SerializeField] private string[] playerNames = { "Anna", "Bence", "Csilla", "Dani" };

        [Header("Conversation")]
        [SerializeField] private NeocortexChatPanel chatPanel;
        [SerializeField] private NeocortexTextChatInput answerInput;
        [SerializeField] private InputField answerField;
        [SerializeField] private Text answerHint;
        [SerializeField] private Button[] optionButtons = Array.Empty<Button>();
        [SerializeField] private Seat[] seats = Array.Empty<Seat>();

        [Header("Question Card")]
        [SerializeField] private Text progressText;
        [SerializeField] private Text keywordText;
        [SerializeField] private Text questionText;
        [SerializeField] private Text upcomingText;
        [SerializeField] private Text statusText;

        [Header("Run")]
        [SerializeField] private Button startButton;
        [SerializeField] private Text startLabel;
        [SerializeField] private Button abortButton;
        [SerializeField] private Button resyncButton;

        [Header("Turn")]
        [SerializeField] private Toggle typedToggle;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button passButton;
        [SerializeField] private Button passRestButton;

        [Header("Character")]
        [Tooltip("Optional. Bools 'Thinking' and 'Talking' drive the host; both false = idle.")]
        [SerializeField] private Animator animator;

        private QuizParticipant[] players = Array.Empty<QuizParticipant>();
        private Color[] verdicts = Array.Empty<Color>();
        private QuizOption[] options = Array.Empty<QuizOption>();
        private int current;
        private string errorText;

        private void Start()
        {
            players = playerNames.Where(n => !string.IsNullOrWhiteSpace(n)).Take(seats.Length)
                .Select((n, i) => new QuizParticipant { id = $"p{i + 1}", displayName = n.Trim() }).ToArray();
            verdicts = new Color[players.Length];
            for (int i = 0; i < seats.Length; i++)
            {
                int seat = i;
                seats[i].button.gameObject.SetActive(i < players.Length);
                if (i < players.Length) seats[i].name.text = players[i].displayName;
                seats[i].button.onClick.AddListener(() => Select(seat));
            }
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int option = i;
                optionButtons[i].onClick.AddListener(() => ChooseOption(option));
            }
            ShowOptions(null);

            answerInput.OnSendButtonClicked.AddListener(text => AnswerAsCurrent(text, typedToggle.isOn));
            startButton.onClick.AddListener(Begin);
            abortButton.onClick.AddListener(() => { quiz.Abort(); Refresh(); });
            resyncButton.onClick.AddListener(() => { errorText = null; quiz.Resync(); });
            hintButton.onClick.AddListener(() => AnswerAsCurrent("Can I have a hint?", false));
            passButton.onClick.AddListener(Pass);
            passRestButton.onClick.AddListener(PassEveryone);

            quiz.OnHostLine.AddListener(line => chatPanel.AddMessage(hostName, line.text, false));
            quiz.OnQuestionChanged.AddListener(ShowQuestion);
            quiz.OnUpcomingQuestion.AddListener(next => upcomingText.text = $"Next scene: {(string.IsNullOrEmpty(next?.keyword) ? "-" : next.keyword)}");
            quiz.OnPendingChanged.AddListener(_ => SelectNext());
            quiz.OnResult.AddListener(ShowResults);
            quiz.OnLeaderboardChanged.AddListener(ShowScores);
            quiz.OnQuizFinished.AddListener(ShowStandings);
            quiz.OnExpectingChanged.AddListener(_ => Refresh());
            quiz.OnBusyChanged.AddListener(_ => Refresh());
            quiz.OnInputDropped.AddListener((text, reason) => chatPanel.AddMessage(null, $"Dropped \"{text}\" ({reason.ToString().ToLower()}).", false));
            quiz.OnRequestFailed.AddListener(error => { errorText = error; Refresh(); });
            quiz.OnCreditsExhausted.AddListener(code => { errorText = $"Out of credits ({code})."; Refresh(); });

            Refresh();
        }

        private void Update()
        {
            // The host's Face Animator follows its lines by itself.
            bool talking = quiz.audioSource != null && quiz.audioSource.isPlaying;

            if (animator == null) return;
            animator.SetBool(Talking, talking);
            animator.SetBool(Thinking, quiz.IsBusy && !talking);
        }

        private bool IsAccepting => !string.IsNullOrEmpty(quiz.SessionId) && !quiz.IsFinished && !quiz.IsBusy
                                    && quiz.Expecting != QuizExpecting.Nothing;

        // What the players can do follows what the host is waiting for.
        private void Refresh()
        {
            bool open = IsAccepting;
            bool started = !string.IsNullOrEmpty(quiz.SessionId);
            bool running = started && !quiz.IsFinished;

            answerField.interactable = open;
            answerHint.text = open && current < players.Length ? $"Reply as {players[current].displayName}..." : "Wait for the question...";
            hintButton.interactable = open && quiz.CurrentQuestion != null && quiz.CurrentQuestion.hasHint;
            passButton.interactable = open;
            passRestButton.interactable = open && players.Length > 1;
            abortButton.interactable = running;
            resyncButton.interactable = running;
            foreach (Button option in optionButtons) option.interactable = open;
            startLabel.text = started ? "Restart" : "Start";

            for (int i = 0; i < players.Length; i++)
            {
                bool judged = verdicts[i].a > 0;
                seats[i].fill.color = i == current ? ChipCurrent : ChipFill;
                seats[i].outline.effectColor = judged ? verdicts[i] : i == current ? Amber : new Color(Amber.r, Amber.g, Amber.b, 0.35f);
            }

            if (!string.IsNullOrEmpty(errorText)) Note(errorText, Bad);
            else if (!started) Note("Press Start to begin the show.", Muted);
            else if (quiz.IsFinished) Note("Show over. Press Restart to play again.", Muted);
            else if (quiz.IsBusy) Note($"{hostName} is thinking...", Muted);
            else if (quiz.Expecting == QuizExpecting.Nothing) Note($"{hostName} is speaking.", Muted);
            else if (quiz.PendingParticipants.Length > 1) Note($"Waiting on {quiz.PendingParticipants.Length} players.", Muted);
            else if (current < players.Length) Note($"Waiting for {players[current].displayName}.", Muted);
        }

        private void Note(string text, Color color)
        {
            statusText.text = text;
            statusText.color = color;
        }

        private void Begin()
        {
            errorText = null;
            if (players.Length == 0 || string.IsNullOrEmpty(quiz.characterID) || string.IsNullOrEmpty(quiz.questionSetID))
            {
                errorText = "Pick a character and a question set in the Samples launcher, or set them on the Quiz Agent.";
                Refresh();
                return;
            }

            chatPanel.ClearMessages();
            questionText.text = keywordText.text = upcomingText.text = progressText.text = "";
            ShowOptions(null);
            for (int i = 0; i < players.Length; i++)
            {
                verdicts[i] = Color.clear;
                seats[i].score.text = "0";
            }
            quiz.Begin(players);
            Refresh();
        }

        private void Select(int index)
        {
            current = Mathf.Clamp(index, 0, Mathf.Max(0, players.Length - 1));
            Refresh();
        }

        // The next player the host still waits on.
        private void SelectNext()
        {
            for (int step = 1; step <= players.Length; step++)
            {
                int index = (current + step) % players.Length;
                string id = players[index].id;
                if (quiz.PendingParticipants.Contains(id) && !quiz.AnsweredParticipants.Contains(id))
                {
                    Select(index);
                    return;
                }
            }
            Refresh();
        }

        private void AnswerAsCurrent(string text, bool typed)
        {
            if (!IsAccepting || current >= players.Length) return;
            errorText = null;
            chatPanel.AddMessage(players[current].displayName, text, true);
            quiz.SayAs(players[current].id, text, typed);
            SelectNext();
        }

        private void ChooseOption(int index)
        {
            if (!IsAccepting || index >= options.Length || current >= players.Length) return;
            errorText = null;
            chatPanel.AddMessage(players[current].displayName, options[index].text, true);
            quiz.ChooseAs(players[current].id, options[index].id);
            SelectNext();
        }

        private void Pass()
        {
            if (!IsAccepting || current >= players.Length) return;
            errorText = null;
            chatPanel.AddMessage(null, $"{players[current].displayName} passes.", false);
            quiz.PassAs(players[current].id);
            SelectNext();
        }

        private void PassEveryone()
        {
            foreach (QuizParticipant player in players.Where(p => quiz.PendingParticipants.Contains(p.id)))
            {
                quiz.PassAs(player.id);
            }
        }

        private void ShowQuestion(QuizQuestion question)
        {
            keywordText.text = $"Scene: {(string.IsNullOrEmpty(question.keyword) ? "-" : question.keyword)}";
            questionText.text = question.questionText;
            progressText.text = $"Question {quiz.Progress.asked} of {quiz.Progress.total}";
            ShowOptions(question.IsChoice ? question.options : null);
            for (int i = 0; i < verdicts.Length; i++) verdicts[i] = Color.clear;
            Select(0);
        }

        private void ShowOptions(QuizOption[] choices)
        {
            options = choices ?? Array.Empty<QuizOption>();
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool used = i < options.Length;
                optionButtons[i].gameObject.SetActive(used);
                if (used) optionButtons[i].GetComponentInChildren<Text>().text = options[i].text;
            }
        }

        private void ShowResults(QuizResult[] results)
        {
            foreach (QuizResult result in results)
            {
                int seat = SeatOf(result.participantId);
                if (seat < 0) continue;
                verdicts[seat] = result.verdict switch
                {
                    QuizVerdict.Correct => Good,
                    QuizVerdict.Partial => Half,
                    QuizVerdict.Incorrect => Bad,
                    _ => Muted
                };
            }
            Refresh();
        }

        private void ShowScores(QuizLeaderboardEntry[] leaderboard)
        {
            foreach (QuizLeaderboardEntry entry in leaderboard ?? Array.Empty<QuizLeaderboardEntry>())
            {
                int seat = SeatOf(entry.participantId);
                if (seat >= 0) seats[seat].score.text = entry.totalScore.ToString();
            }
        }

        private void ShowStandings(QuizLeaderboardEntry[] leaderboard)
        {
            leaderboard ??= Array.Empty<QuizLeaderboardEntry>();
            questionText.text = "Final standings" + string.Concat(leaderboard.OrderBy(e => e.rank).Select(e =>
            {
                int seat = SeatOf(e.participantId);
                return $"\n{e.rank}. {(seat >= 0 ? players[seat].displayName : e.displayName)}  {e.totalScore}";
            }));
            ShowOptions(null);
            ShowScores(leaderboard);
            Refresh();
        }

        private int SeatOf(string participantId)
        {
            return Array.FindIndex(players, p => p.id == participantId);
        }
    }
}
