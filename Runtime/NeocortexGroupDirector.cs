using System.Linq;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Neocortex
{
    /// <summary>
    ///     Runs a group conversation across several <see cref="NeocortexSmartAgent"/>s: sends one group
    ///     turn and routes each speaker's reply back to its own agent. Use <see cref="Send"/> to let the
    ///     cast decide who answers, <see cref="SendTo(NeocortexSmartAgent,string)"/> to address one, or
    ///     <see cref="Continue"/> to let them talk among themselves. A cast of two or more needs a
    ///     Pro/Team API key.
    /// </summary>
    public class NeocortexGroupDirector : MonoBehaviour
    {
        [Tooltip("The characters in the scene. Each is a NeocortexSmartAgent on its own GameObject.")]
        [SerializeField] private List<NeocortexSmartAgent> agents = new();

        [Tooltip("Max characters that speak per director-driven turn before it hands back (1..5).")]
        [Range(1, 5)]
        [SerializeField] private int maxTurns = 5;

        [Header("Group Events")]
        [Tooltip("Raised before a character speaks its turn, use it for a shared transcript / name label.")]
        [Space] public UnityEvent<GroupMessage> OnSpeaker = new();
        [Tooltip("Raised with the whole group turn once, before the speakers are played out.")]
        [Space] public UnityEvent<GroupChatResponse> OnGroupResponseReceived = new();
        [Tooltip("Raised when a turn starts, e.g. to lock the input field while the cast talks.")]
        [Space] public UnityEvent OnTurnStarted = new();
        [Tooltip("Raised when every speaker in the turn has finished.")]
        [Space] public UnityEvent OnTurnFinished = new();
        [Space] public UnityEvent<string> OnRequestFailed = new();
        [Tooltip("Raised with the shared session's transcript when GetHistory() returns, each entry is name-labeled.")]
        [Space] public UnityEvent<ChatHistoryEntry[]> OnHistoryReceived = new();
        [Tooltip("Raised with the transcript of the player's SPOKEN input (SendAudio) so the UI can show it.")]
        [Space] public UnityEvent<string> OnPlayerSpeech = new();

        private ApiRequest apiRequest;
        private bool isBusy;

        /// <summary>The shared scene session id. Empty until the first turn; the server mints it.</summary>
        public string SessionId { get; set; } = "";

        /// <summary>The cast, as assigned. Modify with <see cref="AddAgent"/> / <see cref="RemoveAgent"/>.</summary>
        public IReadOnlyList<NeocortexSmartAgent> Agents => agents;

        /// <summary>True while a group turn is being fetched or played out.</summary>
        public bool IsBusy => isBusy;

        private void Awake()
        {
            apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += HandleRequestFailed;

            // The director speaks for the cast, so no member may answer the player on its own.
            foreach (NeocortexSmartAgent agent in agents)
            {
                if (agent != null) agent.autoVoiceInput = false;
            }
        }

        /// <summary>Adds a character to the cast mid-scene (the server acknowledges the join naturally).</summary>
        public void AddAgent(NeocortexSmartAgent agent)
        {
            if (agent != null && !agents.Contains(agent)) agents.Add(agent);
        }

        /// <summary>Removes a character from the cast mid-scene.</summary>
        public void RemoveAgent(NeocortexSmartAgent agent)
        {
            agents.Remove(agent);
        }

        /// <summary>Forgets the current scene session so the next turn starts a fresh conversation.</summary>
        public void ClearSession() => SessionId = "";

        /// <summary>Player says something to the group; an AI director decides who answers.</summary>
        public void Send(string message) => _ = RunTurn(message, null);

        /// <summary>Player addresses one character; that character answers this turn.</summary>
        public void SendTo(NeocortexSmartAgent agent, string message)
        {
            if (agent == null) return;
            _ = RunTurn(message, agent.characterID);
        }

        /// <summary>Player addresses one character (by id); that character answers this turn.</summary>
        public void SendTo(string characterId, string message) => _ = RunTurn(message, characterId);

        /// <summary>No player input, let the cast talk among themselves (ambient turn).</summary>
        public void Continue() => _ = RunTurn(null, null);

        /// <summary>
        ///     Transcribes the player's speech and sends it to the group like <see cref="Send"/>,
        ///     raising <see cref="OnPlayerSpeech"/> with the transcript.
        /// </summary>
        public async void SendAudio(AudioClip clip)
        {
            if (clip == null) return;

            if (isBusy)
            {
                Debug.LogWarning("[Neocortex] A group turn is already in progress; ignoring input until it finishes.", this);
                return;
            }

            string anyId = agents.FirstOrDefault(a => a != null && !string.IsNullOrEmpty(a.characterID))?.characterID;
            if (string.IsNullOrEmpty(anyId))
            {
                Debug.LogWarning("[Neocortex] Group director has no agents with a Character ID assigned.", this);
                OnTurnFinished.Invoke();
                return;
            }

            // Claim the turn BEFORE awaiting: a second trigger (two listeners on the same mic, a
            // double click) would otherwise start a second transcription and bill for it twice.
            isBusy = true;
            string text = await apiRequest.RequestTranscription(anyId, clip);
            if (this == null) return;
            isBusy = false; // released so RunTurn below can claim it

            // Nothing heard (too short or silent) or the request failed. End the turn anyway, or a
            // UI that locked its mic on send waits forever for a turn that never runs.
            if (string.IsNullOrEmpty(text))
            {
                OnTurnFinished.Invoke();
                return;
            }

            OnPlayerSpeech.Invoke(text);
            _ = RunTurn(text, null);
        }

        /// <summary>
        ///     Loads the scene's transcript (every speaker, oldest first) and raises
        ///     <see cref="OnHistoryReceived"/>. Null before the first turn.
        /// </summary>
        /// <param name="limit">How many messages to fetch.</param>
        /// <param name="before">The previous result's nextCursor, to page back to older messages.</param>
        public async Task<ApiChatHistory> GetHistory(int limit = 20, string before = null)
        {
            if (string.IsNullOrEmpty(SessionId)) return null;

            ApiChatHistory history = await apiRequest.RequestChatHistory(SessionId, limit, before);
            if (this == null) return history;
            if (history != null) OnHistoryReceived.Invoke(history.messages);
            return history;
        }

        private async Task RunTurn(string message, string speakerCharacterId)
        {
            if (isBusy)
            {
                Debug.LogWarning("[Neocortex] A group turn is already in progress; ignoring input until it finishes.", this);
                return;
            }

            string[] characterIds = agents
                .Where(a => a != null && !string.IsNullOrEmpty(a.characterID))
                .Select(a => a.characterID)
                .Distinct()
                .ToArray();

            if (characterIds.Length == 0)
            {
                Debug.LogWarning("[Neocortex] Group director has no agents with a Character ID assigned.", this);
                OnTurnFinished.Invoke();
                return;
            }

            isBusy = true;
            OnTurnStarted.Invoke();

            GroupChatResponse response = await apiRequest.RequestGroupChat(
                characterIds,
                SessionId,
                message,
                speakerCharacterId,
                maxTurns);

            if (this == null) return;

            if (response == null)
            {
                // OnRequestFailed was already raised by ApiRequest.
                isBusy = false;
                OnTurnFinished.Invoke();
                return;
            }

            if (!string.IsNullOrEmpty(response.sessionId)) SessionId = response.sessionId;

            OnGroupResponseReceived.Invoke(response);

            // Play speakers strictly in order: route each message to its agent and wait for that
            // character to finish before the next one starts, so replies don't overlap.
            if (response.messages != null)
            {
                foreach (GroupMessage msg in response.messages)
                {
                    if (this == null) return;

                    OnSpeaker.Invoke(msg);

                    NeocortexSmartAgent speaker = FindAgent(msg.characterId);
                    if (speaker != null)
                    {
                        await speaker.Speak(msg);
                    }
                }
            }

            if (this == null) return;
            isBusy = false;
            OnTurnFinished.Invoke();
        }

        private NeocortexSmartAgent FindAgent(string characterId)
        {
            return agents.FirstOrDefault(a => a != null && a.characterID == characterId);
        }

        private void HandleRequestFailed(string error)
        {
            isBusy = false;
            OnRequestFailed.Invoke(error);
        }
    }
}
