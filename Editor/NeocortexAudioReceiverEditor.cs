using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    [CustomEditor(typeof(NeocortexAudioReceiver))]
    public class NeocortexAudioReceiverEditor : UnityEditor.Editor
    {
        private const string MIC_INDEX_KEY = "neocortex-mic-index";
        
        private string[] microphoneOptions;
        private int selectedMicrophoneIndex;
        
        private void OnEnable()
        {
            microphoneOptions = Microphone.devices;
            selectedMicrophoneIndex = PlayerPrefs.GetInt(MIC_INDEX_KEY, 0);
            
            NeocortexAudioReceiver audioReceiver = (NeocortexAudioReceiver)target;
            audioReceiver.SelectedMicrophone = microphoneOptions[selectedMicrophoneIndex];
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            NeocortexAudioReceiver audioReceiver = (NeocortexAudioReceiver)target;

            if (microphoneOptions is { Length: > 0 })
            {
                selectedMicrophoneIndex = EditorGUILayout.Popup("Select Microphone", selectedMicrophoneIndex, microphoneOptions);
                PlayerPrefs.SetInt(MIC_INDEX_KEY, selectedMicrophoneIndex);
                audioReceiver.SelectedMicrophone = microphoneOptions[selectedMicrophoneIndex];
            }
            else
            {
                EditorGUILayout.LabelField("No microphones available.");
            }
            
            GUILayout.Space(8);
            SerializedProperty onAudioRecorded = serializedObject.FindProperty("OnAudioRecorded");
            EditorGUILayout.PropertyField(onAudioRecorded);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
