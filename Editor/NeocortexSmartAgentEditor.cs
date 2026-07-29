using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;

namespace Neocortex.Editor
{
    /// <summary>
    ///     Inspector for <see cref="NeocortexSmartAgent"/>: replaces the hand-pasted character-id
    ///     string with a dropdown of the team's characters (fetched once per editor session via the
    ///     API key), plus inline setup validation. The underlying field stays a plain string, so
    ///     everything keeps working when the list can't be fetched.
    /// </summary>
    [CustomEditor(typeof(NeocortexSmartAgent))]
    public class NeocortexSmartAgentEditor : UnityEditor.Editor
    {
        private static CharacterSummary[] cachedCharacters;
        private static string fetchError;
        private static bool fetching;

        private SerializedProperty characterIdProperty;

        private void OnEnable()
        {
            characterIdProperty = serializedObject.FindProperty("characterID");

            if (cachedCharacters == null && !fetching && HasApiKey())
            {
                FetchCharacters();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawValidation();
            DrawCharacterPicker();

            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "characterID");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation()
        {
            if (!HasApiKey())
            {
                EditorGUILayout.HelpBox("No API key set, the agent cannot reach Neocortex.", MessageType.Warning);
                if (GUILayout.Button("Open Neocortex Settings"))
                {
                    NeocortexSettingsWindow.ShowWindow();
                }
                EditorGUILayout.Space();
            }
            else if (string.IsNullOrEmpty(characterIdProperty.stringValue))
            {
                EditorGUILayout.HelpBox("Pick the character this agent embodies.", MessageType.Info);
            }
        }

        private void DrawCharacterPicker()
        {
            EditorGUILayout.BeginHorizontal();

            if (cachedCharacters is { Length: > 0 })
            {
                string[] names = cachedCharacters.Select(c => c.name).ToArray();
                int current = Array.FindIndex(cachedCharacters, c => c.id == characterIdProperty.stringValue);

                int picked = EditorGUILayout.Popup("Character", current, names);
                if (picked != current && picked >= 0)
                {
                    characterIdProperty.stringValue = cachedCharacters[picked].id;
                }
            }
            else
            {
                // No list (no key, fetch failed, or empty team): the plain string field is the
                // always-working fallback.
                EditorGUILayout.PropertyField(characterIdProperty, new GUIContent("Character ID"));
            }

            using (new EditorGUI.DisabledScope(fetching || !HasApiKey()))
            {
                if (GUILayout.Button(fetching ? "…" : "↻", GUILayout.Width(28)))
                {
                    FetchCharacters();
                }
            }

            EditorGUILayout.EndHorizontal();

            // The raw id stays visible (and editable) under the dropdown so nothing is hidden.
            if (cachedCharacters is { Length: > 0 })
            {
                EditorGUILayout.PropertyField(characterIdProperty, new GUIContent(" "), GUILayout.ExpandWidth(true));
            }

            if (!string.IsNullOrEmpty(fetchError))
            {
                EditorGUILayout.HelpBox($"Couldn't load your characters: {fetchError}", MessageType.None);
            }
        }

        private static bool HasApiKey()
        {
            NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
            return settings != null && !string.IsNullOrEmpty(settings.apiKey);
        }

        private async void FetchCharacters()
        {
            fetching = true;
            fetchError = null;

            ApiRequest apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += message => fetchError = message;

            ApiCharactersResponse response = await apiRequest.GetCharacters();
            cachedCharacters = response?.characters;
            fetching = false;

            Repaint();
        }
    }
}
