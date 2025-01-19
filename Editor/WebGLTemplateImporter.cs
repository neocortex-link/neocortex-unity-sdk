using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neocortex.Editor
{
    [InitializeOnLoad]
    public class WebGLTemplateImporter
    {
        private const string SourceFolder = "Packages/link.neocortex.sdk/WebGLTemplates/Neocortex";
        private const string DestinationFolder = "Assets/WebGLTemplates/Neocortex";
        private const string ImportCompletedKey = "Neocortex.WebGLTemplateImported";

        static WebGLTemplateImporter()
        {
            EditorApplication.delayCall += OnEditorLoaded;
        }

        [MenuItem("Tools/Neocortex/Import WebGL Template", false, 0)]
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
            if (EditorPrefs.HasKey(ImportCompletedKey) && Directory.Exists(DestinationFolder))
            {
                return;
            }
            
            EditorPrefs.DeleteKey(ImportCompletedKey);

            try{
                CopyDirectory(SourceFolder, DestinationFolder);
                AssetDatabase.Refresh();
                Debug.Log($"WebGL Template copied to {DestinationFolder} successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to copy WebGL Template: {ex.Message}");
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
