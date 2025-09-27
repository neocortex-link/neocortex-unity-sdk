using System;
using Neocortex;
using UnityEditor;
using Neocortex.Data;

[CustomEditor(typeof(NeocortexInteractable))]
public class NeocortexInteractableEditor : Editor
{
    private const int MAX_PROPERTIES = 5;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var script = (NeocortexInteractable)target;

        var typeProperty = serializedObject.FindProperty("Type");
        EditorGUILayout.PropertyField(typeProperty);

        if ((InteractableType)typeProperty.enumValueIndex != InteractableType.CHARACTER)
        {
            var nameProperty = serializedObject.FindProperty("Name");
            EditorGUILayout.PropertyField(nameProperty);
        }
        else
        {
            script.Name = nameof(InteractableType.CHARACTER);
        }

        var propertiesProperty = serializedObject.FindProperty("Properties");
        EditorGUILayout.PropertyField(propertiesProperty, true);

        if (script.Properties is { Length: > MAX_PROPERTIES })
        {
            Array.Resize(ref script.Properties, MAX_PROPERTIES);
            EditorUtility.SetDirty(script);
        }

        if (script.Properties is { Length: MAX_PROPERTIES })
        {
            EditorGUILayout.HelpBox($"Maximum of {MAX_PROPERTIES} properties reached.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}