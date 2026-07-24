using Neocortex;
using UnityEditor;

[CustomEditor(typeof(NeocortexInteractable))]
public class NeocortexInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var script = (NeocortexInteractable)target;

        EditorGUILayout.HelpBox(
            "Characters nearby perceive this automatically — its properties and position go out with every message. " +
            "Properties are name/value pairs: 'name' is seeded from the GameObject (edit, add more, or remove any), " +
            "plus what it is and its state (type=door, color=red, locked=true). " +
            "Put this on a character's own GameObject and it links to that character.",
            MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("properties"), true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
        // Show the short hash a blank id resolves to — the reference characters actually use.
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Resolved Id", script.Id);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
