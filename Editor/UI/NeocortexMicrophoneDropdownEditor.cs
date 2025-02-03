using UnityEditor;
using UnityEngine;

namespace Neocortex
{
    [CustomEditor(typeof(NeocortexMicrophoneDropdown))]
    public class NeocortexMicrophoneDropdownEditor : UnityEditor.Editor
    {
        private const string COMPONENT_NAME_IN_HIERARCY = "Microphone Dropdown";
        
        [MenuItem("GameObject/Neocortex/Microphone Dropdown", false, -int.MaxValue)]
        private static void AddElement()
        {
            EditorUtilities.CreateInCanvas<NeocortexMicrophoneDropdown>(COMPONENT_NAME_IN_HIERARCY, (canvas, target) =>
            {
                var targetTransform = target.transform as RectTransform;
                var canvasTransform = canvas.transform as RectTransform;

                if (targetTransform == null || canvasTransform == null) return;

                targetTransform.anchoredPosition = new Vector2(0, 0);
            });
        }
    }
}
