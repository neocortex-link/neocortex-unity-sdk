using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    /// <summary>
    ///     Inspector for <see cref="NeocortexAudioReceiver"/>: one info box carries the explanation,
    ///     the fields keep short labels. Also owns the microphone picker and the WebGL template
    ///     check, since this is the component those concerns belong to.
    /// </summary>
    [CustomEditor(typeof(NeocortexAudioReceiver))]
    public class NeocortexAudioReceiverEditor : UnityEditor.Editor
    {
        private const string WEBGL_TEMPLATE = "PROJECT:Neocortex";

        private string[] microphoneOptions;
        private int selectedMicrophoneIndex;

        private SerializedProperty usePushToTalk;
        private SerializedProperty amplitudeThreshold;
        private SerializedProperty maxWaitTime;
        private SerializedProperty requestPermissionOnStart;
        private SerializedProperty onAudioRecorded;
        private SerializedProperty onRecordingFailed;
        private SerializedProperty onPermissionGranted;
        private SerializedProperty onPermissionDenied;

        private void OnEnable()
        {
            microphoneOptions = Microphone.devices;
            selectedMicrophoneIndex = PlayerPrefs.GetInt(AudioReceiver.MIC_INDEX_KEY, 0);

            usePushToTalk = serializedObject.FindProperty("usePushToTalk");
            amplitudeThreshold = serializedObject.FindProperty("amplitudeThreshold");
            maxWaitTime = serializedObject.FindProperty("maxWaitTime");
            requestPermissionOnStart = serializedObject.FindProperty("requestPermissionOnStart");
            onAudioRecorded = serializedObject.FindProperty("OnAudioRecorded");
            onRecordingFailed = serializedObject.FindProperty("OnRecordingFailed");
            onPermissionGranted = serializedObject.FindProperty("OnPermissionGranted");
            onPermissionDenied = serializedObject.FindProperty("OnPermissionDenied");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "One microphone for every platform — the right capture backend is picked at runtime, and the mic permission is handled on Android/iOS.\n\n" +
                "Voice activity (default): recording starts when speech rises above the threshold and ends after the silence timeout. " +
                "Push To Talk: records only while the button is held.",
                MessageType.Info);

            EditorGUILayout.PropertyField(usePushToTalk, new GUIContent("Push To Talk"));

            if (!usePushToTalk.boolValue)
            {
                EditorGUILayout.PropertyField(amplitudeThreshold, new GUIContent("Speech Threshold"));
                EditorGUILayout.PropertyField(maxWaitTime, new GUIContent("Silence Timeout"));
            }

            EditorGUILayout.PropertyField(requestPermissionOnStart, new GUIContent("Ask Permission Early"));

            if (microphoneOptions is { Length: > 0 })
            {
                // Track outside changes (runtime dropdown / facade API) and only write on an actual
                // pick — writing every repaint would fight live mic switching.
                selectedMicrophoneIndex = PlayerPrefs.GetInt(AudioReceiver.MIC_INDEX_KEY, 0);
                int picked = EditorGUILayout.Popup("Microphone", selectedMicrophoneIndex, microphoneOptions);
                if (picked != selectedMicrophoneIndex)
                {
                    selectedMicrophoneIndex = picked;
                    PlayerPrefs.SetInt(AudioReceiver.MIC_INDEX_KEY, picked);
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.PropertyField(onAudioRecorded);
            EditorGUILayout.PropertyField(onRecordingFailed);
            EditorGUILayout.PropertyField(onPermissionGranted);
            EditorGUILayout.PropertyField(onPermissionDenied);

            serializedObject.ApplyModifiedProperties();

            // WebGL microphone input rides the Neocortex template's JS.
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL &&
                PlayerSettings.WebGL.template != WEBGL_TEMPLATE)
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("Build target is WebGL but the Neocortex WebGL template is not selected — the microphone will not work in the browser.", MessageType.Warning);
                if (GUILayout.Button("Select Neocortex WebGL Template"))
                {
                    PlayerSettings.WebGL.template = WEBGL_TEMPLATE;
                }
            }
        }
    }
}
