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
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Action Queue", 0)]
    public class NeocortexActionQueue : MonoBehaviour
    {
        [Tooltip("The agent whose replies feed this queue. Falls back to a NeocortexSmartAgent on this GameObject when empty.")]
        [SerializeField] private NeocortexSmartAgent agent;

        private readonly Dictionary<string, Func<ChatResponse, IEnumerator>> handlers = new();
        private readonly Queue<(string keyword, ChatResponse response)> queue = new();
        private bool running;

        /// <summary>Raised when a dequeued action has no registered handler. The action is skipped.</summary>
        public event Action<string> OnUnhandledAction;

        /// <summary>Raised once the queue drains (no more pending actions).</summary>
        public event Action OnQueueCompleted;

        /// <summary>True while an action sequence is currently being processed.</summary>
        public bool IsRunning => running;

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

            agent.OnChatResponseReceived.AddListener(Enqueue);
        }

        private void OnDestroy()
        {
            if (agent != null)
            {
                agent.OnChatResponseReceived.RemoveListener(Enqueue);
            }
        }

        /// <summary>Registers the coroutine that performs an action keyword.</summary>
        public void RegisterAction(string keyword, Func<ChatResponse, IEnumerator> handler)
        {
            handlers[keyword] = handler;
        }

        /// <summary>Queues every stacked action of a reply, in order, and starts processing if idle.</summary>
        public void Enqueue(ChatResponse response)
        {
            if (response?.actions == null)
            {
                Debug.Log("[Neocortex] Action Queue: reply carried no actions array.");
                return;
            }

            foreach (string action in response.actions)
            {
                if (!string.IsNullOrEmpty(action))
                {
                    queue.Enqueue((action, response));
                }
            }

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
                (string keyword, ChatResponse response) = queue.Dequeue();

                if (handlers.TryGetValue(keyword, out Func<ChatResponse, IEnumerator> handler))
                {
                    yield return handler(response);
                }
                else
                {
                    OnUnhandledAction?.Invoke(keyword);
                }
            }

            running = false;
            OnQueueCompleted?.Invoke();
        }
    }
}
