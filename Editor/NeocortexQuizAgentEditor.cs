using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;

namespace Neocortex.Editor
{
    /// <summary>
    ///     Inspector for <see cref="NeocortexQuizAgent"/>. A quiz needs two ids and neither is
    ///     human readable, so both become dropdowns of the team's own characters and question sets,
    ///     fetched once per editor session with the API key. The underlying fields stay plain
    ///     strings, so a missing key, a failed fetch or an empty team leaves a working text box.
    /// </summary>
    [CustomEditor(typeof(NeocortexQuizAgent))]
    public class NeocortexQuizAgentEditor : UnityEditor.Editor
    {
        private static CharacterSummary[] cachedCharacters;
        private static QuestionSetSummary[] cachedQuestionSets;
        private static string fetchError;
        private static bool fetching;

        private SerializedProperty characterIdProperty;
        private SerializedProperty questionSetIdProperty;

        private void OnEnable()
        {
            characterIdProperty = serializedObject.FindProperty("characterID");
            questionSetIdProperty = serializedObject.FindProperty("questionSetID");

            if (cachedCharacters == null && cachedQuestionSets == null && !fetching && HasApiKey())
            {
                Fetch();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawValidation();
            DrawPickers();

            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "characterID", "questionSetID");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation()
        {
            if (!HasApiKey())
            {
                EditorGUILayout.HelpBox("No API key set, the quiz cannot reach Neocortex.", MessageType.Warning);
                if (GUILayout.Button("Open Neocortex Settings"))
                {
                    NeocortexSettingsWindow.ShowWindow();
                }
                EditorGUILayout.Space();
                return;
            }

            bool needsCharacter = string.IsNullOrEmpty(characterIdProperty.stringValue);
            bool needsSet = string.IsNullOrEmpty(questionSetIdProperty.stringValue);

            if (needsCharacter && needsSet)
            {
                EditorGUILayout.HelpBox("Pick the character that hosts the quiz and the question set it plays.", MessageType.Info);
            }
            else if (needsCharacter)
            {
                EditorGUILayout.HelpBox("Pick the character that hosts the quiz.", MessageType.Info);
            }
            else if (needsSet)
            {
                EditorGUILayout.HelpBox("Pick the question set this quiz plays.", MessageType.Info);
            }
        }

        private void DrawPickers()
        {
            EditorGUILayout.BeginHorizontal();
            DrawCharacterField();
            DrawRefreshButton();
            EditorGUILayout.EndHorizontal();

            // The raw id stays visible and editable under each dropdown so nothing is hidden.
            if (cachedCharacters is { Length: > 0 })
            {
                EditorGUILayout.PropertyField(characterIdProperty, new GUIContent(" "));
            }

            DrawQuestionSetField();

            if (cachedQuestionSets is { Length: > 0 })
            {
                EditorGUILayout.PropertyField(questionSetIdProperty, new GUIContent(" "));
            }

            if (!string.IsNullOrEmpty(fetchError))
            {
                EditorGUILayout.HelpBox($"Couldn't load your lists: {fetchError}", MessageType.None);
            }
        }

        private void DrawCharacterField()
        {
            if (cachedCharacters is not { Length: > 0 })
            {
                EditorGUILayout.PropertyField(characterIdProperty, new GUIContent("Character ID"));
                return;
            }

            string[] names = cachedCharacters.Select(c => c.name).ToArray();
            int current = Array.FindIndex(cachedCharacters, c => c.id == characterIdProperty.stringValue);

            int picked = EditorGUILayout.Popup("Character", current, names);
            if (picked != current && picked >= 0)
            {
                characterIdProperty.stringValue = cachedCharacters[picked].id;
            }
        }

        private void DrawQuestionSetField()
        {
            if (cachedQuestionSets is not { Length: > 0 })
            {
                EditorGUILayout.PropertyField(questionSetIdProperty, new GUIContent("Question Set ID"));
                return;
            }

            // The question count is what tells two similarly named sets apart at a glance.
            string[] names = cachedQuestionSets
                .Select(s => $"{s.name} ({s.questionCount} question{(s.questionCount == 1 ? "" : "s")})")
                .ToArray();
            int current = Array.FindIndex(cachedQuestionSets, s => s.id == questionSetIdProperty.stringValue);

            int picked = EditorGUILayout.Popup("Question Set", current, names);
            if (picked != current && picked >= 0)
            {
                questionSetIdProperty.stringValue = cachedQuestionSets[picked].id;
            }
        }

        private void DrawRefreshButton()
        {
            using (new EditorGUI.DisabledScope(fetching || !HasApiKey()))
            {
                if (GUILayout.Button(fetching ? "…" : "↻", GUILayout.Width(28)))
                {
                    Fetch();
                }
            }
        }

        private static bool HasApiKey()
        {
            NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
            return settings != null && !string.IsNullOrEmpty(settings.apiKey);
        }

        private async void Fetch()
        {
            fetching = true;
            fetchError = null;

            ApiRequest apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += message => fetchError = message;

            ApiCharactersResponse characters = await apiRequest.GetCharacters();
            cachedCharacters = characters?.characters;

            ApiQuestionSetsResponse sets = await apiRequest.GetQuestionSets();
            cachedQuestionSets = sets?.questionSets;

            fetching = false;
            Repaint();
        }
    }
}
