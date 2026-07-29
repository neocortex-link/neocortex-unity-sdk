using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    /// <summary>
    ///     The live microphone readout shared by the audio receiver inspectors: input level against
    ///     the speech threshold, the silence timeout draining to zero, and the current state.
    /// </summary>
    public static class AudioReceiverMonitor
    {
        // Matches the runtime amplitude bar, so the Inspector and the in-game widget agree.
        private const float AMPLITUDE_SCALE = 5f;

        /// <summary>Draws the readout. Live in Play Mode, a short hint otherwise.</summary>
        /// <param name="mic">The receiver being inspected.</param>
        public static void Draw(AudioReceiver mic)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to watch the microphone level and what it is hearing.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.LabelField("State", State(mic));
            DrawInputLevel(mic);

            // The silence timeout only ends a voice-activity recording; push-to-talk ends on release.
            if (!mic.usePushToTalk)
            {
                DrawSilenceCountdown(mic);
            }

            EditorGUILayout.EndVertical();
        }

        // Busy means the microphone is shut, which in a normal turn is the character thinking or
        // speaking. Push-to-talk is always ready for a press, so it never reads busy.
        private static string State(AudioReceiver mic)
        {
            if (mic.IsUserSpeaking) return "Recording";
            if (mic.IsListening || mic.usePushToTalk) return "Waiting User Input";
            return "Busy";
        }

        // The marker shows where speech is detected, so it is obvious whether talking crosses it.
        private static void DrawInputLevel(AudioReceiver mic)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(mic.Amplitude * AMPLITUDE_SCALE), $"Input   {mic.Amplitude:F3}");

            float marker = Mathf.Clamp01(mic.amplitudeThreshold * AMPLITUDE_SCALE);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width * marker, rect.y, 1f, rect.height), Color.red);
        }

        // Drains to zero while the player is silent; the recording is sent when it empties.
        private static void DrawSilenceCountdown(AudioReceiver mic)
        {
            float remaining = Mathf.Clamp01(1f - mic.ElapsedWaitTime);
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, remaining, $"Silence {remaining * mic.maxWaitTime:F1}s left");
        }
    }
}
