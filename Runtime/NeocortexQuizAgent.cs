using System;
using System.Collections.Generic;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;
using System.Threading.Tasks;

namespace Neocortex
{
    /// <summary>
    ///     Runs a quiz, a game show or a spoken test hosted by one of your characters.
    ///
    ///     Point it at a character and a question set and call <see cref="Begin"/>. After that the
    ///     only thing a game does is hand over what the player said, with <see cref="Say"/> or
    ///     <see cref="Choose"/>. Question order, retries, hints, scoring and when the run ends are
    ///     decided on the server from the question set, so none of that lives in your scene.
    ///
    ///     Listen to <see cref="OnQuestionChanged"/> to build the scene and
    ///     <see cref="OnUpcomingQuestion"/> to prepare the next one while the host is still talking.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Quiz Agent", 2)]
    public class NeocortexQuizAgent : MonoBehaviour
    {
        [Tooltip("The character (project) id that hosts the quiz.")]
        public string characterID;

        [Tooltip("The question set to play. Create one under Question Sets in the dashboard.")]
        public string questionSetID;

        [Tooltip("Your own id for this player. Leave empty to use this device.")]
        public string playerID;

        [Tooltip("Start the quiz as soon as the scene runs.")]
        public bool beginOnStart;

        [Tooltip("Speak the host's lines out loud using the character's voice.")]
        public bool speakHostLines = true;

        [Tooltip("Where the host's voice plays. Falls back to an AudioSource on this GameObject.")]
        public AudioSource audioSource;

        [Header("Input")]
        [Tooltip("Hold what the player says while the host is busy, and send it once the host is ready.")]
        public bool queueInputWhileBusy = true;

        [Tooltip("How long a held answer stays worth sending, in seconds. Anything older is dropped.")]
        public float maxPendingInputAge = 4f;

        [Header("Host")]
        [Tooltip("Raised for each line the host says, as that line starts, with the emotion to play it in.")]
        [Space] public UnityEvent<QuizLine> OnHostLine = new();
        [Tooltip("Raised when the emotion changes. Drive animation here.")]
        [Space] public UnityEvent<Emotions> OnEmotionChanged = new();
        [Tooltip("Raised once the host has finished speaking this turn.")]
        [Space] public UnityEvent OnHostFinished = new();

        [Header("Scene")]
        [Tooltip("Raised as the host starts asking, so the scene is set on the right line. Build the scene from its keyword.")]
        [Space] public UnityEvent<QuizQuestion> OnQuestionChanged = new();
        [Tooltip("Raised with the question after this one, so you can load its scene early.")]
        [Space] public UnityEvent<QuizUpcoming> OnUpcomingQuestion = new();

        [Header("Play")]
        [Tooltip("Raised with what the game should collect next. Open the microphone on anything but Nothing.")]
        [Space] public UnityEvent<QuizExpecting> OnExpectingChanged = new();
        [Tooltip("Raised when a turn judged an answer.")]
        [Space] public UnityEvent<QuizResult[]> OnResult = new();
        [Tooltip("Raised whenever the standings change.")]
        [Space] public UnityEvent<QuizLeaderboardEntry[]> OnLeaderboardChanged = new();
        [Tooltip("Raised with the whole turn, if you would rather read it yourself.")]
        [Space] public UnityEvent<QuizTurnResponse> OnTurnReceived = new();
        [Tooltip("Raised once the host has signed off.")]
        [Space] public UnityEvent<QuizLeaderboardEntry[]> OnQuizFinished = new();
        [Tooltip("Raised true while a turn is in flight and false when it lands. Drive a spinner here.")]
        [Space] public UnityEvent<bool> OnBusyChanged = new();
        [Tooltip("Raised with anything the player said that never reached the host, and why.")]
        [Space] public UnityEvent<string, QuizInputDropReason> OnInputDropped = new();
        [Space] public UnityEvent<string> OnRequestFailed = new();

        /// <summary>The run in progress, or null before <see cref="Begin"/>.</summary>
        public string SessionId { get; private set; }

        /// <summary>The question on the table, or null before the quiz starts and once it ends.</summary>
        public QuizQuestion CurrentQuestion { get; private set; }

        /// <summary>
        ///     Which question is on the table and how many there are. Lives here rather than on
        ///     the question, so it is still readable while the host is greeting or signing off.
        /// </summary>
        public QuizProgress Progress { get; private set; } = new QuizProgress();

        /// <summary>What the game should be collecting right now.</summary>
        public QuizExpecting Expecting { get; private set; } = QuizExpecting.Nothing;

        /// <summary>True while a turn is in flight. Ignore player input until it clears.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>True once the host has signed off.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>Something the player said, held until the host is ready for it.</summary>
        private struct PendingInput
        {
            public string text;
            public string optionId;
            public string participantId;
            public float at;

            /// <summary>What to report if this one is dropped: what the player actually said.</summary>
            public string Spoken => text ?? optionId ?? string.Empty;
        }

        private ApiRequest apiRequest;

        /// <summary>
        ///     Identifies the turn in flight. Every point where this class resumes after an await
        ///     checks it, so a turn abandoned by a restart, a scene change or a destroyed host
        ///     stops there instead of finishing into an object that is no longer around.
        /// </summary>
        private int turnToken;

        private PendingInput pending;
        private bool hasPending;
        private Emotions lastEmotion;
        private bool emotionRaised;

        private void Awake()
        {
            apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += error => OnRequestFailed?.Invoke(error);

            if (speakHostLines && audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void Start()
        {
            if (beginOnStart)
            {
                Begin();
            }
        }

        private void OnDestroy()
        {
            // The token bump is what stops a turn already in the air from calling back into a
            // destroyed component; aborting the request stops it wasting the round trip too.
            turnToken++;
            apiRequest?.Abort();
            StopSpeaking();
        }

        /// <summary>Starts the quiz. The host welcomes the players and asks the first question.</summary>
        public void Begin()
        {
            // Starting again from scratch is a fair thing to ask for. Two runs answering into
            // the same scene at once is not, so whatever is in flight is let go first.
            Abort();

            IsFinished = false;
            CurrentQuestion = null;
            Progress = new QuizProgress();
            emotionRaised = false;

            QuizParticipant[] participants = null;
            RunTurn(() => apiRequest.BeginQuiz(characterID, questionSetID, playerID, participants));
        }

        /// <summary>
        ///     Abandons whatever is in flight and silences the host, without ending the run on the
        ///     server. The session can be picked up again with <see cref="Resync"/>.
        /// </summary>
        public void Abort()
        {
            turnToken++;
            apiRequest?.Abort();
            StopSpeaking();
            DropPending(QuizInputDropReason.Superseded);
            SetBusy(false);
            SetExpecting(QuizExpecting.Nothing);
        }

        /// <summary>
        ///     Hands the host whatever the player said, from speech to text or a text box.
        ///
        ///     An empty string is a deliberate pass: the question is recorded unanswered and the
        ///     run moves on, without a model call and without a credit. Do NOT send an empty
        ///     transcription this way. Speech the recogniser could not make out is an utterance,
        ///     not a decision to skip, and the host handles it as one: it asks again, offers the
        ///     clue, and only then moves on.
        /// </summary>
        public void Say(string text)
        {
            Send(new PendingInput { text = text, at = Time.realtimeSinceStartup });
        }

        /// <summary>Hands the host a tapped choice, for a multiple choice or true or false question.</summary>
        public void Choose(string optionId)
        {
            Send(new PendingInput { optionId = optionId, at = Time.realtimeSinceStartup });
        }

        /// <summary>The same as <see cref="Say"/>, naming which player spoke in a multi player game.</summary>
        public void SayAs(string participantId, string text)
        {
            Send(new PendingInput { text = text, participantId = participantId, at = Time.realtimeSinceStartup });
        }

        /// <summary>
        ///     Reads the run back from the server and puts this agent in step with it.
        ///
        ///     Used after a turn whose response never arrived: the server may well have applied
        ///     that turn, so which question is on the table and what the host is waiting for are
        ///     its truth to tell rather than something to guess at here. Whatever the player said
        ///     on the lost turn is never sent again.
        /// </summary>
        public async void Resync()
        {
            if (string.IsNullOrEmpty(SessionId)) return;

            int token = ++turnToken;
            SetBusy(true);

            QuizPublicState state = null;

            try
            {
                ApiQuizSessionResponse session = await apiRequest.GetQuizSession(SessionId);
                if (this == null || token != turnToken) return;

                state = session?.state;
                if (state == null) return;

                IsFinished = state.IsFinished;
                CurrentQuestion = state.activeQuestion;
                Progress = new QuizProgress
                {
                    asked = Mathf.Max(0, state.totalQuestions - state.questionsRemaining),
                    total = state.totalQuestions
                };

                if (state.leaderboard is { Length: > 0 })
                {
                    OnLeaderboardChanged?.Invoke(state.leaderboard);
                }

                if (CurrentQuestion != null)
                {
                    OnQuestionChanged?.Invoke(CurrentQuestion);
                }

                if (IsFinished)
                {
                    OnQuizFinished?.Invoke(state.leaderboard ?? Array.Empty<QuizLeaderboardEntry>());
                }
            }
            finally
            {
                if (this != null && token == turnToken)
                {
                    SetBusy(false);
                    // Last, so a game reopening input on this event does so against an agent
                    // that has already settled.
                    SetExpecting(state?.expecting ?? QuizExpecting.Nothing);
                }
            }
        }

        private void Send(PendingInput input)
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                OnRequestFailed?.Invoke("Call Begin() before sending answers to the quiz.");
                OnInputDropped?.Invoke(input.Spoken, QuizInputDropReason.NoSession);
                return;
            }

            if (IsFinished)
            {
                OnRequestFailed?.Invoke("This quiz has already finished.");
                OnInputDropped?.Invoke(input.Spoken, QuizInputDropReason.Finished);
                return;
            }

            if (IsBusy)
            {
                if (!queueInputWhileBusy)
                {
                    OnInputDropped?.Invoke(input.Spoken, QuizInputDropReason.Busy);
                    return;
                }

                // Last one wins. A player who says "triangle... TRIANGLE" means one answer, and
                // the freshest words are the ones aimed at what the host has just asked; a queue
                // would answer question two with question one's answer.
                DropPending(QuizInputDropReason.Superseded);
                pending = input;
                hasPending = true;
                return;
            }

            RunTurn(() => apiRequest.ContinueQuiz(SessionId, input.text, input.optionId, input.participantId));
        }

        /// <summary>
        ///     Runs one turn from end to end: busy on, request, apply, busy off.
        ///
        ///     All of it is inside a try/finally because everything in between raises events into
        ///     game code, and a listener that throws must not leave the agent busy and the
        ///     microphone shut for the rest of the run.
        /// </summary>
        private async void RunTurn(Func<Task<QuizTurnResponse>> request)
        {
            int token = ++turnToken;
            bool failed = false;

            SetBusy(true);
            // Nothing is collected while a turn is in the air.
            SetExpecting(QuizExpecting.Nothing);

            try
            {
                QuizTurnResponse turn = await request();
                if (this == null || token != turnToken) return;

                if (turn == null)
                {
                    failed = true;
                    return;
                }

                await Apply(turn, token);
            }
            finally
            {
                if (this != null && token == turnToken)
                {
                    SetBusy(false);

                    if (failed)
                    {
                        DropPending(QuizInputDropReason.Failed);
                        Resync();
                    }
                    else
                    {
                        FlushPending();
                    }
                }
            }
        }

        private void FlushPending()
        {
            if (!hasPending) return;

            PendingInput next = pending;
            hasPending = false;

            if (IsFinished || Expecting == QuizExpecting.Nothing)
            {
                OnInputDropped?.Invoke(next.Spoken, QuizInputDropReason.Finished);
                return;
            }

            // An answer to a question the host has since moved past is worse than no answer at
            // all, so anything that waited out a whole turn is let go rather than sent late.
            if (Time.realtimeSinceStartup - next.at > maxPendingInputAge)
            {
                OnInputDropped?.Invoke(next.Spoken, QuizInputDropReason.Expired);
                return;
            }

            Send(next);
        }

        private void DropPending(QuizInputDropReason reason)
        {
            if (!hasPending) return;

            hasPending = false;
            OnInputDropped?.Invoke(pending.Spoken, reason);
        }

        private async Task Apply(QuizTurnResponse turn, int token, bool resumedFromWelcome = false)
        {
            SessionId = turn.sessionId ?? SessionId;
            OnTurnReceived?.Invoke(turn);

            // A turn carries the question only when it changes, so a retry or a confirmation
            // leaves what is already on screen alone. Its arrival IS the change.
            bool questionChanged = turn.question != null;
            if (questionChanged)
            {
                CurrentQuestion = turn.question;
                Progress = turn.progress ?? Progress;
            }
            else if (turn.done)
            {
                CurrentQuestion = null;
                Progress = turn.progress ?? Progress;
            }

            if (turn.upcoming != null)
            {
                OnUpcomingQuestion?.Invoke(turn.upcoming);
            }

            if (turn.result is { Length: > 0 })
            {
                OnResult?.Invoke(turn.result);
            }

            if (turn.leaderboard is { Length: > 0 })
            {
                OnLeaderboardChanged?.Invoke(turn.leaderboard);
            }

            // The run is over the moment the turn says so, even though the host still has its
            // sign off to deliver. Anything reading IsFinished while those last lines play gets
            // the truth rather than "still playing".
            IsFinished = turn.done;

            // The welcome: the host has greeted the player and the first question comes next.
            // That turn is asked for NOW, while the greeting is still being spoken, because the
            // server has already persisted this one and released its lock. It is a second or
            // two the player would otherwise spend listening to nothing.
            //
            // A missing question does NOT mean the welcome. A retry offer, a clue, a repeat and
            // a turn the host could not make out all leave the question where it was and send
            // no new one. What marks the welcome is that there is nothing for the player to do
            // yet: expecting is Nothing. Keying on the missing question alone sent a turn with
            // nothing said into an open question, which the server rightly refused with 422
            // nothing_said, on every single wrong answer that earned another go.
            //
            // resumedFromWelcome stops a server that keeps answering with no question looping.
            bool isWelcome = !turn.done
                && turn.question == null
                && turn.expecting == QuizExpecting.Nothing
                && !resumedFromWelcome;
            Task<QuizTurnResponse> upNext = isWelcome ? apiRequest.ContinueQuiz(SessionId) : null;

            await SpeakLines(turn.lines, questionChanged ? turn.question : null, token);
            if (this == null || token != turnToken) return;

            OnHostFinished?.Invoke();

            if (upNext != null)
            {
                QuizTurnResponse first = await upNext;
                if (this == null || token != turnToken) return;

                if (first != null)
                {
                    await Apply(first, token, true);
                    return;
                }

                // The greeting was spoken but the question never arrived. Nothing here can say
                // what the host is waiting for now, so the run is read back instead of guessed.
                Resync();
                return;
            }

            if (turn.done)
            {
                OnQuizFinished?.Invoke(turn.leaderboard ?? Array.Empty<QuizLeaderboardEntry>());
                return;
            }

            SetExpecting(turn.expecting);
        }

        private void SetBusy(bool next)
        {
            if (IsBusy == next) return;

            IsBusy = next;
            OnBusyChanged?.Invoke(next);
        }

        private void SetExpecting(QuizExpecting next)
        {
            if (Expecting == next) return;

            Expecting = next;
            OnExpectingChanged?.Invoke(next);
        }

        private void RaiseEmotion(Emotions emotion)
        {
            if (emotionRaised && lastEmotion == emotion) return;

            emotionRaised = true;
            lastEmotion = emotion;
            OnEmotionChanged?.Invoke(emotion);
        }

        /// <summary>
        ///     Says the host's lines in order, putting <paramref name="pendingQuestion"/> on screen
        ///     as the line that asks it begins. A turn that both reacts to the last answer and asks
        ///     the next question would otherwise change the scene while the host is still talking
        ///     about the last one. If no line claims to be the question, the scene is set before
        ///     the first line rather than never.
        ///
        ///     Each line's speech is requested while the line before it is still playing, so the
        ///     gap between two lines is a frame rather than a whole round trip.
        /// </summary>
        private async Task SpeakLines(QuizLine[] lines, QuizQuestion pendingQuestion, int token)
        {
            if (lines == null)
            {
                RevealQuestion(ref pendingQuestion);
                return;
            }

            // Only the lines with something to say, so the lookahead is never aimed at a blank.
            List<QuizLine> spoken = new List<QuizLine>();
            bool anyAsks = false;
            foreach (QuizLine line in lines)
            {
                if (line == null || string.IsNullOrEmpty(line.text)) continue;
                if (line.IsQuestion) anyAsks = true;
                spoken.Add(line);
            }

            if (!anyAsks) RevealQuestion(ref pendingQuestion);

            bool voicing = speakHostLines && audioSource != null && !string.IsNullOrEmpty(characterID);
            Task<AudioClip> clipRequest = voicing && spoken.Count > 0 ? RequestClip(spoken[0]) : null;

            for (int i = 0; i < spoken.Count; i++)
            {
                QuizLine line = spoken[i];
                AudioClip clip = null;

                if (voicing)
                {
                    clip = clipRequest == null ? null : await clipRequest;
                    if (this == null || token != turnToken)
                    {
                        DestroyClip(clip);
                        return;
                    }

                    clipRequest = i + 1 < spoken.Count ? RequestClip(spoken[i + 1]) : null;
                }

                // Raised as the line starts rather than when the turn arrived, so a caption and
                // the voice saying it are one event.
                if (line.IsQuestion) RevealQuestion(ref pendingQuestion);
                OnHostLine?.Invoke(line);
                RaiseEmotion(line.emotion);

                if (clip == null) continue;

                await PlayClip(clip, token);
                DestroyClip(clip);

                if (this == null || token != turnToken)
                {
                    DiscardClip(clipRequest);
                    return;
                }
            }

            RevealQuestion(ref pendingQuestion);
        }

        private Task<AudioClip> RequestClip(QuizLine line)
        {
            // spokenText is the phonetic reading the host supplies when a line has numbers
            // in it, so "+100" is heard as "plus one hundred points".
            string speech = string.IsNullOrEmpty(line.spokenText) ? line.text : line.spokenText;
            return apiRequest.GenerateAudio(characterID, speech, line.emotion.ToString());
        }

        private async Task PlayClip(AudioClip clip, int token)
        {
            audioSource.clip = clip;
            audioSource.Play();

            // A deadline rather than waiting on isPlaying alone: a paused AudioListener, a
            // disabled source or a zeroed timeScale each leave that flag in a state this loop
            // would never come out of, which hangs the whole turn.
            float pitch = Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch));
            float deadline = Time.realtimeSinceStartup + (clip.length / pitch) + 0.25f;

            while (audioSource.isPlaying && Time.realtimeSinceStartup < deadline)
            {
                if (this == null || token != turnToken) return;
                await Task.Yield();
            }
        }

        private void StopSpeaking()
        {
            if (audioSource == null) return;

            AudioClip clip = audioSource.clip;
            audioSource.Stop();
            audioSource.clip = null;
            DestroyClip(clip);
        }

        /// <summary>
        ///     Every spoken line is a freshly downloaded clip. Left alone they pile up for the
        ///     life of the scene, twenty to forty of them over a single run.
        /// </summary>
        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null) Destroy(clip);
        }

        /// <summary>Throws away a clip that was already on its way when the turn was abandoned.</summary>
        private static async void DiscardClip(Task<AudioClip> request)
        {
            if (request == null) return;

            DestroyClip(await request);
        }

        private void RevealQuestion(ref QuizQuestion pendingQuestion)
        {
            if (pendingQuestion == null) return;

            QuizQuestion question = pendingQuestion;
            pendingQuestion = null;
            OnQuestionChanged?.Invoke(question);
        }
    }
}
