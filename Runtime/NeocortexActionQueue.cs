using System;
using UnityEngine;
using Neocortex.Data;
using System.Collections;
using System.Collections.Generic;

namespace Neocortex
{
    /// <summary>
    ///     Runs the stacked actions a reply carries (<c>response.actions</c>) one at a time, in the
    ///     order the character intends them. Register a coroutine handler per action keyword (as
    ///     authored on the character's Actions node); replies are captured from the agent
    ///     automatically. Unhandled actions are skipped and surfaced via
    ///     <see cref="OnUnhandledAction"/>.
    ///
    ///     <see cref="trigger"/> decides WHEN the queue runs: the instant the reply arrives, or only
    ///     after the character has finished speaking it (so a "walk over there" doesn't start before
    ///     the voice line does).
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Action Queue", 0)]
    public class NeocortexActionQueue : MonoBehaviour
    {
        /// <summary>When the queued actions of a reply are run.</summary>
        public enum ActionTrigger
        {
            /// <summary>Run as soon as the reply arrives — the character may act before it starts speaking.</summary>
            WhenResponseReceived,
            /// <summary>Run once the character has finished speaking the reply (text + audio).</summary>
            AfterReplySpoken,
            /// <summary>Run the moment the character starts speaking — action and voice line begin together.</summary>
            WhenSpeechStarts,
        }

        [Tooltip("The agent whose replies feed this queue. Falls back to a NeocortexSmartAgent on this GameObject when empty.")]
        [SerializeField] private NeocortexSmartAgent agent;

        [Tooltip("When Response Received: actions run the instant the reply arrives (may act before speaking).\n" +
                 "When Speech Starts: actions run as the character begins the voice line — movement and speech together.\n" +
                 "After Reply Spoken: actions wait until the character finishes speaking.\n" +
                 "The last two need an audio Chat Lines Mode on the agent (Single/Per-Line Audio); Off mode raises no speech events and falls back to firing on arrival.")]
        [SerializeField] private ActionTrigger trigger = ActionTrigger.WhenResponseReceived;

        private readonly Dictionary<string, Func<ChatAction, IEnumerator>> handlers = new();
        private readonly Queue<ChatAction> queue = new();
        private bool running;
        private bool warnedOffMode;

        /// <summary>Raised when a dequeued action has no registered handler. The action is skipped.</summary>
        public event Action<string> OnUnhandledAction;

        /// <summary>Raised once the queue drains (no more pending actions).</summary>
        public event Action OnQueueCompleted;

        /// <summary>True while an action sequence is currently being processed.</summary>
        public bool IsRunning => running;

        /// <summary>When the queued actions run. Settable from code before the first reply.</summary>
        public ActionTrigger Trigger
        {
            get => trigger;
            set => trigger = value;
        }

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NeocortexSmartAgent>();
            }

            if (agent == null)
            {
                Debug.LogError("[Neocortex] Action Queue needs a NeocortexSmartAgent — assign one in the Inspector or put the queue on the agent's GameObject.", this);
                enabled = false;
                return;
            }

            // Always capture a reply's actions the moment it arrives; the deferred triggers each
            // wait for their OWN playback event (speech start vs reply finished). Capture decides
            // the fire-now case (and the Off-mode fallback) at reply time, after Awake/Start settle.
            agent.OnChatResponseReceived.AddListener(Capture);
            switch (trigger)
            {
                case ActionTrigger.WhenSpeechStarts:
                    agent.OnChatLineStarted.AddListener(StartPendingOnLine);
                    break;
                case ActionTrigger.AfterReplySpoken:
                    agent.OnReplyFinished.AddListener(StartPending);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (agent != null)
            {
                agent.OnChatResponseReceived.RemoveListener(Capture);
                agent.OnChatLineStarted.RemoveListener(StartPendingOnLine);
                agent.OnReplyFinished.RemoveListener(StartPending);
            }
        }

        /// <summary>
        ///     Registers the coroutine that performs an action keyword. The handler receives the
        ///     action, whose <c>targetId</c> is the entity it applies to (empty when none).
        /// </summary>
        public void RegisterAction(string keyword, Func<ChatAction, IEnumerator> handler)
        {
            handlers[keyword] = handler;
        }

        /// <summary>Queues every stacked action of a reply, in order, then runs them now or after the reply is spoken.</summary>
        public void Capture(ChatResponse response)
        {
            if (response?.actions == null)
            {
                Debug.Log("[Neocortex] Action Queue: reply carried no actions array.");
                return;
            }

            foreach (ChatAction action in response.actions)
            {
                if (!string.IsNullOrEmpty(action?.name))
                {
                    queue.Enqueue(action);
                }
            }

            // The deferred triggers wait for a playback event (speech start / reply finished). Off
            // mode does no playback and raises neither, so waiting would hang the queue forever —
            // fire on arrival instead, once, with a heads-up.
            bool deferred = trigger != ActionTrigger.WhenResponseReceived;

            if (deferred && agent.ChatLinesMode == ChatLinesMode.Off)
            {
                if (!warnedOffMode)
                {
                    warnedOffMode = true;
                    Debug.LogWarning("[Neocortex] Action Queue is set to fire on speech (When Speech Starts / After Reply Spoken), but the agent's Chat Lines Mode is Off, which raises no speech events. Firing on response instead — set Single or Per-Line Audio.", this);
                }
                deferred = false;
            }

            if (!deferred)
            {
                StartPending();
            }
        }

        // Speech-start listener wrapper (OnChatLineStarted carries the line; the queue ignores it).
        private void StartPendingOnLine(ChatLine _) => StartPending();

        // Kicks off processing if idle and there is anything queued. Safe to call more than once
        // (e.g. captured-now AND a later playback event) — the running/empty guards no-op.
        private void StartPending()
        {
            if (!running && queue.Count > 0)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            running = true;

            while (queue.Count > 0)
            {
                ChatAction action = queue.Dequeue();

                if (handlers.TryGetValue(action.name, out Func<ChatAction, IEnumerator> handler))
                {
                    yield return handler(action);
                }
                else
                {
                    OnUnhandledAction?.Invoke(action.name);
                }
            }

            running = false;
            OnQueueCompleted?.Invoke();
        }
    }
}
