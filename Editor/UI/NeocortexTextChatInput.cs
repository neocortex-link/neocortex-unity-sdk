using UnityEditor;
using UnityEngine;

namespace Neocortex
{
    [CustomEditor(typeof(NeocortexTextChatInput))]
    public class NeocortexTextChatInputEditor : UnityEditor.Editor
    {
        private const string COMPONENT_NAME_IN_HIERARCY = "Text Chat Input";
        
        [MenuItem("GameObject/Neocortex/Text Chat Input", false, -int.MaxValue)]
        private static void AddElement()
        {
            EditorUtilities.CreateInCanvas<NeocortexTextChatInput>(COMPONENT_NAME_IN_HIERARCY, (canvas, target) =>
            {
                var targetTransform = target.transform as RectTransform;
                var canvasTransform = canvas.transform as RectTransform;

                if (targetTransform == null || canvasTransform == null) return;

                targetTransform.anchoredPosition = new Vector2(0, 0);
            });
        }
    }
}
