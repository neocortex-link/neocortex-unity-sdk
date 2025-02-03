using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neocortex
{
    public class EditorUtilities : MonoBehaviour
    {
        private const string CANVAS_FILE_NAME = "Canvas";
        private const string PREFAB_FOLDER_NAME = "Prefabs";
        private const string EVENT_SYSTEM_FILE_NAME = "Event System";

        /// <summary>
        ///     Secure loading of a UI item. Checks if a <see cref="Canvas"/> and an <see cref="EventSystem"/> component exists in the scene and creates if necessary.
        /// </summary>
        /// <typeparam name="T">Type of component to be loaded.</typeparam>
        /// <param name="name">Name of the object to be created.</param>
        /// <param name="callback">Actions to take once component is created in the canvas.</param>
        public static void CreateInCanvas<T>(string name, Action<Canvas, T> callback) where T: UIBehaviour
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (Selection.activeTransform == null)
            {
                if (canvas == null)
                {
                    canvas = LoadAndInstantiate<Canvas>(CANVAS_FILE_NAME);
                }
                Selection.activeObject = canvas;
            }
            else
            {
                var rect = Selection.activeTransform.GetComponentInParent<RectTransform>();

                if (rect == null)
                {
                    canvas = LoadAndInstantiate<Canvas>(CANVAS_FILE_NAME, Selection.activeTransform);
                    Selection.activeObject = canvas;
                }
                else
                {
                    Selection.activeObject = rect;
                }
            }

            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = LoadAndInstantiate<EventSystem>(EVENT_SYSTEM_FILE_NAME);
            }

            var target = LoadAndInstantiate<T>(name, Selection.activeTransform);

            Selection.activeObject = target;

            var undoGroup = Undo.GetCurrentGroup();
            var undoGroupName = Guid.NewGuid().ToString();
            Undo.RegisterCreatedObjectUndo(canvas.gameObject, undoGroupName);
            Undo.RegisterCreatedObjectUndo(target.gameObject, undoGroupName);
            Undo.RegisterCreatedObjectUndo(eventSystem.gameObject, undoGroupName);
            Undo.CollapseUndoOperations(undoGroup);

            callback?.Invoke(canvas, target);
        }

        /// <summary>
        ///     Load a prefab from the <see cref="PREFAB_FOLDER_NAME"/> folder.
        /// </summary>
        /// <param name="fileName">Name of the prefab to load.</param>
        /// <param name="parent">Transform parent of the prefab to instantiate.</param>
        /// <typeparam name="T">Type of the prefab.</typeparam>
        /// <returns>The prefab instance.</returns>
        public static T LoadAndInstantiate<T>(string fileName, Transform parent = null) where T : Behaviour
        {
            var prefab = Resources.Load<T>($"{PREFAB_FOLDER_NAME}/{fileName}");
            
            if(prefab == null)
            {
                throw new Exception($"Prefab {fileName} not found in {PREFAB_FOLDER_NAME}.");
            }
            
            var instance = Instantiate(prefab);
            instance.name = fileName;
            
            var transform = instance.transform;
            transform.SetParent(parent);
            transform.SetAsLastSibling();
            
            EditorUtility.SetDirty(instance);
            return instance;
        }
    }
}
