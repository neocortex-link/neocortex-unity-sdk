using UnityEditor;
using UnityEngine;
using Neocortex.API;

namespace Neocortex.Editor
{
    public class NeocortexAccountWindow : EditorWindow
    {
        private ApiAccountResponse account;
        private string error;
        private bool isLoading;

        [MenuItem("Tools/Neocortex/Account Status", false, 1)]
        public static void ShowWindow()
        {
            NeocortexAccountWindow window = GetWindow<NeocortexAccountWindow>("Neocortex Account");
            window.minSize = new Vector2(512, 160);
            window.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private async void Refresh()
        {
            isLoading = true;
            error = null;
            Repaint();

            // A fresh ApiRequest each time so an API key saved after the window opened is picked up.
            ApiRequest apiRequest = new ApiRequest();
            apiRequest.OnRequestFailed += message => error = message;

            ApiAccountResponse response = await apiRequest.GetAccount();
            if (response != null)
            {
                account = response;
            }

            isLoading = false;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Account Status", EditorStyles.boldLabel);

            if (account != null)
            {
                EditorGUILayout.LabelField("Tier", account.tier);
                EditorGUILayout.LabelField("Owner", account.email);
                EditorGUILayout.LabelField("Credits Remaining", account.creditsRemaining.ToString("N0"));
                EditorGUILayout.LabelField("Next Refresh", account.nextRefresh.HasValue
                    ? account.nextRefresh.Value.ToLocalTime().ToString("f")
                    : "-");
            }

            if (isLoading)
            {
                EditorGUILayout.HelpBox("Loading account info...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            EditorGUILayout.Space();

            GUI.enabled = !isLoading;
            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }
    }
}
