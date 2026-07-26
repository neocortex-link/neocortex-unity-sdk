using UnityEditor;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;

namespace Neocortex.Editor
{
    /// <summary>
    ///     One window for everything account-related: set the API key and view the account status
    ///     (tier, owner, credits, next refresh) it unlocks.
    /// </summary>
    public class NeocortexSettingsWindow : EditorWindow
    {
        private static NeocortexSettings settings;

        private ApiAccountResponse account;
        private ApiCharactersResponse characters;
        private string accountError;
        private Vector2 charactersScroll;

        [MenuItem("Tools/Neocortex/Settings", false, 0)]
        public static void ShowWindow()
        {
            NeocortexSettingsWindow window = GetWindow<NeocortexSettingsWindow>("Neocortex Settings");
            window.minSize = new Vector2(512, 420);
            window.maxSize = new Vector2(512, 1000);
            window.Show();
        }

        private void OnEnable()
        {
            settings = NeocortexSettingsProvider.GetOrCreate();

            // Only fetch when a key exists, so opening the window without one isn't a noisy error.
            if (!string.IsNullOrEmpty(settings.apiKey))
            {
                RefreshAccount();
            }
        }

        private void OnGUI()
        {
            DrawAccountSection();
            EditorGUILayout.Space();
            DrawApiKeySection();
            EditorGUILayout.Space();
            DrawCharactersSection();
        }

        private void DrawAccountSection()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Account Status", EditorStyles.boldLabel);

            // Always render every field; show a dash when there is no value so the layout is stable.
            EditorGUILayout.LabelField("Tier", Field(account?.tier));
            EditorGUILayout.LabelField("Owner", Field(account?.email));
            EditorGUILayout.LabelField("Credits Remaining", account != null ? account.creditsRemaining.ToString("N0") : "-");
            EditorGUILayout.LabelField("Next Refresh", account != null && account.nextRefresh.HasValue
                ? account.nextRefresh.Value.ToLocalTime().ToString("f")
                : "-");

            EditorGUILayout.Space();

            // Always render exactly one status box so this area never appears or disappears.
            (string message, MessageType type) = AccountStatus();
            EditorGUILayout.HelpBox(message, type);

            EditorGUILayout.EndVertical();
        }

        private (string message, MessageType type) AccountStatus()
        {
            if (!string.IsNullOrEmpty(accountError)) return (accountError, MessageType.Error);
            if (account == null) return ("Save an API key below to load your account status.", MessageType.Info);
            return ("Account status is up to date.", MessageType.Info);
        }

        private static string Field(string value) => string.IsNullOrEmpty(value) ? "-" : value;
        
        private void DrawApiKeySection()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("API Key Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            settings.apiKey = EditorGUILayout.PasswordField("API Key", settings.apiKey, GUILayout.ExpandWidth(true));

            // very light green
            GUI.color = new Color(0.8f, 1f, 0.8f, 1f);
            if (GUILayout.Button("Save", GUILayout.Width(64)))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();

                // Re-fetch so the account section below reflects the key just saved.
                RefreshAccount();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("Create", GUILayout.Width(64)))
            {
                Application.OpenURL("https://neocortex.link/dashboard/api-keys");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can create an API key in the Neocortex Dashboard, API Keys page.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawCharactersSection()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Your Characters", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(64)) && !string.IsNullOrEmpty(settings.apiKey))
            {
                RefreshAccount();
            }
            EditorGUILayout.EndHorizontal();

            if (characters?.characters is { Length: > 0 })
            {
                charactersScroll = EditorGUILayout.BeginScrollView(charactersScroll, GUILayout.MaxHeight(140));
                foreach (CharacterSummary character in characters.characters)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(character.name, GUILayout.Width(180));
                    EditorGUILayout.LabelField(character.id, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Copy ID", GUILayout.Width(64)))
                    {
                        EditorGUIUtility.systemCopyBuffer = character.id;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(settings.apiKey)
                        ? "Save an API key to list your characters."
                        : "No characters yet, create one in the dashboard, then Refresh.",
                    MessageType.Info);
                if (GUILayout.Button("Open Dashboard"))
                {
                    Application.OpenURL("https://neocortex.link/dashboard/characters");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private async void RefreshAccount()
        {
            account = null;
            characters = null;
            accountError = null;
            Repaint();

            // A fresh ApiRequest each time so a key saved after the window opened is picked up.
            ApiRequest apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += message => accountError = message;

            ApiAccountResponse response = await apiRequest.GetAccount();
            if (response != null)
            {
                account = response;
                characters = await apiRequest.GetCharacters();
            }
            // If response is null (request failed), account stays null, so UI shows dashes + error.

            Repaint();
        }
    }
}
