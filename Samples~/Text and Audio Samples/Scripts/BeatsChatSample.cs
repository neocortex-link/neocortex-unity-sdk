using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Text chat using chat beats: each reply arrives as ordered per-emotion chunks shown as
    ///     separate bubbles. Enable expressiveAudio to also voice every beat in its own emotion —
    ///     note this costs ~1 audio credit PER BEAT (up to 4x a normal reply) instead of 1.
    /// </summary>
    public class BeatsChatSample : MonoBehaviour
    {
        [Tooltip("OPT-IN: voice each beat separately (~1 credit each) for progressive, emotion-synced speech.")]
        [SerializeField] private bool expressiveAudio;

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent agent;
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;

        private void Start()
        {
            agent.BeatsMode = expressiveAudio ? BeatsMode.ExpressiveAudio : BeatsMode.TextAndEmotion;

            agent.OnBeatStarted.AddListener(OnBeatStarted);
            agent.OnEmotionChanged.AddListener(emotion => Debug.Log($"[EMOTION] {emotion}"));
            chatInput.OnSendButtonClicked.AddListener(Submit);
        }

        private void OnBeatStarted(Beat beat)
        {
            thinkingIndicator.Display(false);
            chatPanel.AddMessage(beat.text, false);
        }

        private void Submit(string message)
        {
            chatPanel.AddMessage(message, true);

            // Queued automatically if the character is still speaking; sent when the reply finishes.
            agent.TextToText(message);
            thinkingIndicator.Display(true);
        }
    }
}
