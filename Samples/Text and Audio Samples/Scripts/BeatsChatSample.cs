using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Samples
{
    /// <summary>
    ///     Text chat using chat beats: each reply arrives as ordered per-emotion chunks shown as
    ///     separate bubbles. Enable perBeatAudio to voice every beat in its own emotion — note this
    ///     costs 1 audio credit PER BEAT (up to 4x a normal reply) instead of 1.
    /// </summary>
    public class BeatsChatSample : MonoBehaviour
    {
        [Tooltip("OPT-IN: one audio request per beat (~1 credit each) for progressive, emotion-synced speech.")]
        [SerializeField] private bool perBeatAudio;

        [Header("Neocortex Components")]
        [SerializeField] private NeocortexSmartAgent smartAgent;
        [SerializeField] private NeocortexBeatPlayer beatPlayer;
        [SerializeField] private NeocortexTextChatInput chatInput;
        [SerializeField] private NeocortexThinkingIndicator thinkingIndicator;
        [SerializeField] private NeocortexChatPanel chatPanel;

        private void Start()
        {
            smartAgent.RequestBeats = true;
            beatPlayer.PlaybackMode = perBeatAudio ? BeatPlaybackMode.PerBeatAudio : BeatPlaybackMode.TextReveal;

            beatPlayer.OnBeatStarted.AddListener(OnBeatStarted);
            beatPlayer.OnEmotionChanged.AddListener(emotion => Debug.Log($"[EMOTION] {emotion}"));
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

            // Queued automatically if the NPC is still speaking its beats; sent when it finishes.
            beatPlayer.SendText(message);
            thinkingIndicator.Display(true);
        }
    }
}
