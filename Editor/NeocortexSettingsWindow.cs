using UnityEditor;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Editor
{
    public class NeocortexSettingsWindow : EditorWindow
    {
        private static NeocortexSettings settings;

        [MenuItem("Tools/Neocortex/API Key Setup", false, 0)]
        public static void ShowWindow()
        {
            NeocortexSettingsWindow window = GetWindow<NeocortexSettingsWindow>("Neocortex Settings");
            window.minSize = window.maxSize = new Vector2(512, 96);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");

            if (settings == null)
            {
                if (!System.IO.Directory.Exists("Assets/Resources/Neocortex"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Resources/Neocortex");
                }
                
                settings = CreateInstance<NeocortexSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Resources/Neocortex/NeocortexSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("API Key Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            settings.apiKey = EditorGUILayout.PasswordField("API Key", settings.apiKey, GUILayout.ExpandWidth(true));

            // very light green
            GUI.color = new Color(0.8f, 1f, 0.8f, 1f );
            if (GUILayout.Button("Save", GUILayout.Width(64)))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
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
    }
}