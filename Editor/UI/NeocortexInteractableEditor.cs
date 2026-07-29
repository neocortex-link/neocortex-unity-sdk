using Neocortex;
using UnityEditor;

[CustomEditor(typeof(NeocortexInteractable))]
public class NeocortexInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var script = (NeocortexInteractable)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("properties"), true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
        // Show the short hash a blank id resolves to, the reference characters actually use.
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Resolved Id", script.Id);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
