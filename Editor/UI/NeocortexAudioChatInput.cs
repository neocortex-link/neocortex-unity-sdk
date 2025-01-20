using UnityEditor;
using UnityEngine;

namespace Neocortex
{
    [CustomEditor(typeof(NeocortexAudioChatInput))]
    public class NeocortexAudioChatInputEditor : UnityEditor.Editor
    {
        private const string COMPONENT_NAME_IN_HIERARCY = "Audio Chat Input";
        
        [MenuItem("GameObject/Neocortex/Audio Chat Input", false, -int.MaxValue)]
        private static void AddElement()
        {
            EditorUtilities.CreateInCanvas<NeocortexAudioChatInput>(COMPONENT_NAME_IN_HIERARCY, (canvas, target) =>
            {
                var targetTransform = target.transform as RectTransform;
                var canvasTransform = canvas.transform as RectTransform;

                if (targetTransform == null || canvasTransform == null) return;

                targetTransform.anchoredPosition = new Vector2(0, 0);
            });
        }
    }
}
