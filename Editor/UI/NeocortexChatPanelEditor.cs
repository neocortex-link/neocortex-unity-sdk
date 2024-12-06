using UnityEditor;
using UnityEngine;

namespace Neocortex
{
    [CustomEditor(typeof(NeocortexChatPanel))]
    public class NeocortexChatPanelEditor : UnityEditor.Editor
    {
        private const string COMPONENT_NAME_IN_HIERARCY = "Chat Panel";
        
        [MenuItem("GameObject/Neocortex/Chat Panel", false, -int.MaxValue)]
        private static void AddElement()
        {
            EditorUtilities.CreateInCanvas<NeocortexChatPanel>(COMPONENT_NAME_IN_HIERARCY, (canvas, target) =>
            {
                var targetTransform = target.transform as RectTransform;
                var canvasTransform = canvas.transform as RectTransform;

                if (targetTransform == null || canvasTransform == null) return;

                targetTransform.anchoredPosition = new Vector2(0, 0);
            });
        }
    }
}
