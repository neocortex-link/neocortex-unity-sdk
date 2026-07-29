using UnityEditor;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex.Editor
{
    /// <summary>
    ///     Single owner of the settings asset: any editor code can get (or lazily create) it here,
    ///     so it exists even if the settings window was never opened. Also opens the settings window
    ///     once on first import when no API key is set, the "paste your key" onboarding moment.
    /// </summary>
    public static class NeocortexSettingsProvider
    {
        private const string ResourcesFolder = "Assets/Resources/Neocortex";
        private const string AssetPath = ResourcesFolder + "/NeocortexSettings.asset";

        // EditorPrefs is machine-wide; scope the first-run flag to this project.
        private static string FirstRunKey => $"Neocortex.SettingsWindowShown.{Application.dataPath.GetHashCode():X}";

        public static NeocortexSettings GetOrCreate()
        {
            NeocortexSettings settings = Resources.Load<NeocortexSettings>("Neocortex/NeocortexSettings");
            if (settings != null)
            {
                return settings;
            }

            if (!System.IO.Directory.Exists(ResourcesFolder))
            {
                System.IO.Directory.CreateDirectory(ResourcesFolder);
            }

            settings = ScriptableObject.CreateInstance<NeocortexSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        [InitializeOnLoadMethod]
        private static void AutoOpenOnFirstRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(FirstRunKey, false))
                {
                    return;
                }

                EditorPrefs.SetBool(FirstRunKey, true);

                if (string.IsNullOrEmpty(GetOrCreate().apiKey))
                {
                    NeocortexSettingsWindow.ShowWindow();
                }
            };
        }
    }
}
