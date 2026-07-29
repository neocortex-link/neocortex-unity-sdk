using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    [InitializeOnLoad]
    public class WebGLTemplateImporter
    {
        private const string DestinationFolder = "Assets/WebGLTemplates/Neocortex";
        private const string ImportCompletedKey = "Neocortex.WebGLTemplateImported";
        private const string TemplateName = "PROJECT:Neocortex";

        // Resolve the template source from wherever the package actually lives (UPM cache,
        // embedded package, or this dev repo where the SDK sits under Assets/).
        private static string SourceFolder
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(WebGLTemplateImporter).Assembly);
                if (package != null)
                {
                    return Path.Combine(package.resolvedPath, "WebGLTemplates/Neocortex");
                }

                return "Assets/neocortex-unity-sdk/WebGLTemplates/Neocortex";
            }
        }

        static WebGLTemplateImporter()
        {
            EditorApplication.delayCall += OnEditorLoaded;
        }

        [MenuItem("Tools/Neocortex/Import WebGL Template", false, 1)]
        public static void ImportWebGLTemplate()
        {
            if (EditorUtility.DisplayDialog("Import WebGL Template", "This will overwrite any changes you have made in the WebGL template. Are you sure you want to continue?", "Yes", "No"))
            {
                if (Directory.Exists(DestinationFolder))
                {
                    Directory.Delete(DestinationFolder, recursive: true);
                }
                
                OnEditorLoaded();
            }
        }
        
        private static void OnEditorLoaded()
        {
            WarnIfTemplateNotSelected();

            if (EditorPrefs.HasKey(ImportCompletedKey) && Directory.Exists(DestinationFolder))
            {
                return;
            }
            
            EditorPrefs.DeleteKey(ImportCompletedKey);

            try
            {
                CopyDirectory(SourceFolder, DestinationFolder);
                AssetDatabase.Refresh();
                Debug.Log($"WebGL Template copied to {DestinationFolder} successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to copy WebGL Template: {ex.Message}");
            }
        }

        // Voice input on WebGL needs the Neocortex template (it ships the microphone JS).
        // One actionable warning instead of a silent runtime failure.
        private static void WarnIfTemplateNotSelected()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL &&
                Directory.Exists(DestinationFolder) &&
                PlayerSettings.WebGL.template != TemplateName)
            {
                Debug.LogWarning("[Neocortex] Build target is WebGL but the Neocortex WebGL template is not selected. " +
                                 "Microphone input will not work. Select it under Project Settings > Player > WebGL > Resolution and Presentation > Neocortex.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
            
            EditorPrefs.SetBool(ImportCompletedKey, true);
        }
    }
}
