using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    [CustomEditor(typeof(AudioReceiver))]
    public class AudioReceiverEditor : UnityEditor.Editor
    {
        private const string MIC_INDEX_KEY = "neocortex-mix-index";
        
        private string[] microphoneOptions;
        private int selectedMicrophoneIndex;
        
        private void OnEnable()
        {
            microphoneOptions = Microphone.devices;
            selectedMicrophoneIndex = PlayerPrefs.GetInt(MIC_INDEX_KEY, 0);
            
            AudioReceiver audioReceiver = (AudioReceiver)target;
            audioReceiver.SelectedMicrophone = microphoneOptions[selectedMicrophoneIndex];
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            AudioReceiver audioReceiver = (AudioReceiver)target;

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
        }
    }
}
