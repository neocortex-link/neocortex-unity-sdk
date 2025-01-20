using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    [CustomEditor(typeof(AudioReceiver), true)]
    public class AudioReceiverEditor : UnityEditor.Editor
    {
        private const string MIC_INDEX_KEY = "neocortex-mic-index";
        
        private string[] microphoneOptions;
        private int selectedMicrophoneIndex;
        
        private SerializedProperty onAudioRecorded;
        private SerializedProperty onRecordingFailed;
        
        private void OnEnable()
        {
            microphoneOptions = Microphone.devices;
            selectedMicrophoneIndex = PlayerPrefs.GetInt(MIC_INDEX_KEY, 0);

            AudioReceiver audioReceiver = (AudioReceiver)target;
            
            if(audioReceiver is NeocortexAudioReceiver neocortexAudioReceiver)
                neocortexAudioReceiver.SelectedMicrophone = microphoneOptions[selectedMicrophoneIndex];
            
            onAudioRecorded = serializedObject.FindProperty("OnAudioRecorded");
            onRecordingFailed = serializedObject.FindProperty("OnRecordingFailed");
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            AudioReceiver audioReceiver = (AudioReceiver)target;
            
            if (microphoneOptions is { Length: > 0 } && audioReceiver is NeocortexAudioReceiver neocortexAudioReceiver)
            {
                selectedMicrophoneIndex = EditorGUILayout.Popup("Select Microphone", selectedMicrophoneIndex, microphoneOptions);
                PlayerPrefs.SetInt(MIC_INDEX_KEY, selectedMicrophoneIndex);
                neocortexAudioReceiver.SelectedMicrophone = microphoneOptions[selectedMicrophoneIndex];
            }
            else
            {
                EditorGUILayout.LabelField("No microphones available.");
            }
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(onAudioRecorded);
            serializedObject.ApplyModifiedProperties();
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(onRecordingFailed);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
