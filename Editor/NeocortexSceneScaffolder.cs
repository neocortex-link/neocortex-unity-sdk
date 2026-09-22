using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Neocortex.Data;

namespace Neocortex.Editor
{
    /// <summary>
    ///     One-click chat rigs: places the UI widgets, creates the character object (agent + audio
    ///     + voice input), and wires every reference through <see cref="NeocortexChatUI"/>, so the
    ///     only thing left to do is pick the character and press Play.
    /// </summary>
    public static class NeocortexSceneScaffolder
    {
        [MenuItem("GameObject/Neocortex/Complete Text Chat", false, -2000)]
        public static void CreateCompleteTextChat() => Scaffold(withVoice: false);

        [MenuItem("GameObject/Neocortex/Complete Voice Chat", false, -1999)]
        public static void CreateCompleteVoiceChat() => Scaffold(withVoice: true);

        private static void Scaffold(bool withVoice)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = EditorUtilities.LoadAndInstantiate<Canvas>("Canvas");
            }

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = EditorUtilities.LoadAndInstantiate<EventSystem>("Event System");
            }

            // Widgets, from the same prefabs the individual menu items use.
            NeocortexChatPanel panel = EditorUtilities.LoadAndInstantiate<NeocortexChatPanel>("Chat Panel", canvas.transform);
            panel.name = "Neocortex Chat Panel";
            if (panel.transform is RectTransform panelRect)
            {
                panelRect.anchoredPosition = Vector2.zero;
            }

            NeocortexTextChatInput textInput = !withVoice
                ? EditorUtilities.LoadAndInstantiate<NeocortexTextChatInput>("Text Chat Input", canvas.transform)
                : null;
            if (textInput != null)
            {
                textInput.name = "Neocortex Text Chat Input";
                ((RectTransform)textInput.transform).anchoredPosition = new Vector2(0, -188);
            }
            
            NeocortexThinkingIndicator thinking = EditorUtilities.LoadAndInstantiate<NeocortexThinkingIndicator>("Thinking Indicator", canvas.transform);
            thinking.name = "Neocortex Thinking Indicator";
            ((RectTransform)thinking.transform).anchoredPosition = new Vector2(0, 180);
            
            NeocortexAudioChatInput audioInput = withVoice
                ? EditorUtilities.LoadAndInstantiate<NeocortexAudioChatInput>("Audio Chat Input", canvas.transform)
                : null;
            if (audioInput != null)
            {
                audioInput.name = "Neocortex Audio Chat Input";
                ((RectTransform)audioInput.transform).anchoredPosition = new Vector2(0, -188);
            }

            // The character: agent + audio output (+ voice input) + the UI binder, one object.
            GameObject characterGO = new GameObject("Neocortex Character");
            NeocortexSmartAgent agent = characterGO.AddComponent<NeocortexSmartAgent>();
            AudioSource audioSource = characterGO.AddComponent<AudioSource>();
            NeocortexAudioReceiver voiceInput = withVoice ? characterGO.AddComponent<NeocortexAudioReceiver>() : null;
            NeocortexChatUI chatUI = characterGO.AddComponent<NeocortexChatUI>();

            // Serialized wiring, so it persists in the scene exactly as if hand-dragged.
            SerializedObject agentSo = new SerializedObject(agent);
            agentSo.FindProperty("audioSource").objectReferenceValue = audioSource;
            agentSo.FindProperty("chatLinesMode").enumValueIndex = (int)(withVoice ? ChatLinesMode.SingleAudio : ChatLinesMode.Text);
            agentSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject uiSo = new SerializedObject(chatUI);
            uiSo.FindProperty("agent").objectReferenceValue = agent;
            uiSo.FindProperty("chatPanel").objectReferenceValue = panel;
            uiSo.FindProperty("textInput").objectReferenceValue = textInput;
            uiSo.FindProperty("thinkingIndicator").objectReferenceValue = thinking;

            if (withVoice)
            {
                uiSo.FindProperty("audioInput").objectReferenceValue = audioInput;
                uiSo.FindProperty("voiceInput").objectReferenceValue = voiceInput;

                SerializedObject audioInputSo = new SerializedObject(audioInput);
                audioInputSo.FindProperty("voiceInput").objectReferenceValue = voiceInput;
                audioInputSo.ApplyModifiedPropertiesWithoutUndo();
            }

            uiSo.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(characterGO, "Create Neocortex Chat");

            // Land the developer exactly where the one remaining step is: picking the character.
            Selection.activeObject = characterGO;
            EditorGUIUtility.PingObject(agent);
            Debug.Log("[Neocortex] Chat rig created and wired. Pick your character on the 'Neocortex Character' object, then press Play.", characterGO);
        }
    }
}
