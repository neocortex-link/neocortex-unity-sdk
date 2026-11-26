using System;
using System.IO;
using System.Linq;
using Neocortex.API;
using Neocortex.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace Neocortex.Samples
{
    /// <summary>
    ///     The sample launcher: opens the sample whose card you click, and fills the character and
    ///     question set pickers from your team, so every sample uses the character you pick and the
    ///     quiz plays the question set you pick. The microphone picker saves its own choice. The
    ///     sample scenes must sit next to this one.
    /// </summary>
    public class SampleLauncher : MonoBehaviour
    {
        [Serializable]
        private class SampleCard
        {
            [Tooltip("The sample scene's file name, without .unity.")]
            public string scene;

            public Button card;
        }

        [SerializeField] private Dropdown characterPicker;
        [SerializeField] private Dropdown questionSetPicker;
        [SerializeField] private Text status;
        [SerializeField] private Color errorColor = new(0.93f, 0.42f, 0.42f, 1);

        [Tooltip("The inactive Back button canvas, carried into each sample this opens.")]
        [SerializeField] private SampleNavigator navigator;

        [SerializeField] private SampleCard[] samples;

        private CharacterSummary[] characters = { };
        private QuestionSetSummary[] questionSets = { };
        private Color statusColor;
        private bool opening;

        private void Start()
        {
            statusColor = status.color;
            characterPicker.onValueChanged.AddListener(_ => SavePick());
            questionSetPicker.onValueChanged.AddListener(_ => SaveQuestionSet());
            foreach (SampleCard sample in samples)
            {
                sample.card.onClick.AddListener(() => Open(sample.scene));
            }
            LoadCharacters();
        }

        private async void LoadCharacters()
        {
            NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
            if (settings == null || string.IsNullOrWhiteSpace(settings.apiKey))
            {
                SetStatus("Add your API key in Tools > Neocortex > Settings, then play again.", true);
                return;
            }

            string error = null;
            ApiRequest request = new ApiRequest();
            request.OnRequestFailed += message => error = message;
            ApiCharactersResponse response = await request.GetCharacters();
            if (this == null) return;

            if (response?.characters == null)
            {
                SetStatus(error ?? "Could not load your characters.", true);
                return;
            }
            if (response.characters.Length == 0)
            {
                SetStatus("No characters on this key yet. Create one in your Neocortex dashboard.", true);
                return;
            }

            characters = response.characters;
            characterPicker.options = characters.Select(c => new Dropdown.OptionData(c.name)).ToList();
            characterPicker.SetValueWithoutNotify(Mathf.Max(0, Array.FindIndex(characters, c => c.id == SampleNavigator.SavedCharacterId)));
            characterPicker.RefreshShownValue();
            characterPicker.interactable = true;
            SavePick();
            SetStatus("", false);

            ApiQuestionSetsResponse sets = await request.GetQuestionSets();
            if (this == null || sets?.questionSets == null || sets.questionSets.Length == 0)
            {
                if (this != null) questionSetPicker.captionText.text = "No question sets";
                return;
            }
            questionSets = sets.questionSets;
            questionSetPicker.options = questionSets.Select(q => new Dropdown.OptionData(q.name)).ToList();
            questionSetPicker.SetValueWithoutNotify(Mathf.Max(0, Array.FindIndex(questionSets, q => q.id == SampleNavigator.SavedQuestionSetId)));
            questionSetPicker.RefreshShownValue();
            questionSetPicker.interactable = true;
            SaveQuestionSet();
        }

        // The group sample needs each character once, so the others join in list order.
        private void SavePick()
        {
            if (characterPicker.value < 0 || characterPicker.value >= characters.Length) return;
            CharacterSummary picked = characters[characterPicker.value];
            SampleNavigator.SaveCharacter(picked.id, picked.name, characters.Select(c => c.id).Where(id => id != picked.id));
        }

        private void SaveQuestionSet()
        {
            if (questionSetPicker.value < 0 || questionSetPicker.value >= questionSets.Length) return;
            SampleNavigator.SaveQuestionSet(questionSets[questionSetPicker.value].id);
        }

        private void Open(string scene)
        {
            if (opening) return;
            string launcherPath = gameObject.scene.path;
            string path = ScenePath(scene);
            if (SceneUtility.GetBuildIndexByScenePath(path) < 0 && !Application.isEditor)
            {
                SetStatus($"Add \"{scene}\" to the build's scene list to open it here.", true);
                return;
            }
            opening = true;
            navigator.Open(launcherPath, path);
        }

        private string ScenePath(string scene)
        {
            return $"{Path.GetDirectoryName(gameObject.scene.path)?.Replace('\\', '/')}/{scene}.unity";
        }

        private void SetStatus(string message, bool isError)
        {
            status.text = message;
            status.color = isError ? errorColor : statusColor;
        }

#if UNITY_EDITOR
        [ContextMenu("Add Samples To Build Settings")]
        private void AddToBuildSettings()
        {
            string launcherPath = gameObject.scene.path;
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != launcherPath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(launcherPath, true));
            foreach (string path in samples.Select(s => ScenePath(s.scene)))
            {
                if (scenes.All(s => s.path != path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Neocortex samples added to Build Settings, with the launcher first.");
        }
#endif
    }
}
