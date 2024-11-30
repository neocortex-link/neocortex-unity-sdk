using System;
using UnityEditor;
using UnityEngine;

namespace Neocortex
{
    [CustomEditor(typeof(NeocortexThinkingIndicator))]
    public class NeocortexThinkingIndicatorEditor : UnityEditor.Editor
    {
        private const string COMPONENT_NAME_IN_HIERARCY = "Thinking Indicator";
        
        [MenuItem("GameObject/Neocortex/Thinking Indicator", false, -int.MaxValue)]
        private static void AddElement()
        {
            EditorUtilities.CreateInCanvas<NeocortexThinkingIndicator>(COMPONENT_NAME_IN_HIERARCY, (canvas, target) =>
            {
                var targetTransform = target.transform as RectTransform;
                var canvasTransform = canvas.transform as RectTransform;

                if (targetTransform == null || canvasTransform == null) return;

                targetTransform.anchoredPosition = new Vector2(0, 0);
            });
        }
    }
}
