using UnityEditor;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Editor
{
    public class NeocortexSettingsWindow : EditorWindow
    {
        private static NeocortexSettings settings;

        [MenuItem("Tools/Neocortex Settings", false, 0)]
        public static void ShowWindow()
        {
            NeocortexSettingsWindow window = GetWindow<NeocortexSettingsWindow>("Neocortex Settings");
            window.minSize = window.maxSize = new Vector2(512, 124);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            settings = Resources.Load<NeocortexSettings>("NeocortexSettings");

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
            GUILayout.Label("API Key", EditorStyles.boldLabel);
            settings.apiKey = EditorGUILayout.PasswordField("Enter API Key", settings.apiKey);

            if (GUILayout.Button("Save API Key"))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}