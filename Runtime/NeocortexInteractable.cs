using System;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     Makes this GameObject part of what characters perceive; nearby characters carry it with
    ///     every message automatically. Characters reference it by <see cref="Id"/>, never by name.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Interactable", 0)]
    public class NeocortexInteractable : MonoBehaviour
    {
        [Tooltip("Unique id characters use to reference this. Leave empty to derive a stable one from the scene path.")]
        [SerializeField] private string id;

        [Tooltip("Everything about this object as name/value pairs.")]
        [SerializeField] private InteractableProperty[] properties = Array.Empty<InteractableProperty>();

        /// <summary>Stable identity: the authored id, or one derived from this object's scene path.</summary>
        public string Id => string.IsNullOrEmpty(id) ? DerivedId() : id;

        public InteractableProperty[] Properties => properties;

        /// <summary>The "name" property's value, or "" if the developer removed it.</summary>
        public string Name => GetProperty("name");

        private NeocortexSmartAgent agent;

        // Seed a "name" property when the component is first added.
        private void Reset()
        {
            properties = new[] { new InteractableProperty { name = "name", value = gameObject.name } };
        }

        private void Awake()
        {
            // An Interactable on a character's GameObject IS that character, so it can recognise
            // itself and others see it as a character rather than a prop.
            agent = GetComponent<NeocortexSmartAgent>();
        }

        public Interactable ToInteractable()
        {
            bool isCharacter = agent != null;

            return new Interactable
            {
                id = Id,
                properties = properties,
                characterId = agent != null ? agent.characterID : null,
                name = Name,
                position = transform.position,
                // Legacy split, still echoed by the API; derived so nobody has to set it.
                type = isCharacter ? "CHARACTER" : "OBJECT",
                isSubject = false,
            };
        }

        private string GetProperty(string key)
        {
            if (properties == null) return "";

            foreach (InteractableProperty property in properties)
            {
                if (string.Equals(property.name, key, StringComparison.OrdinalIgnoreCase)) return property.value;
            }

            return "";
        }

        // Hierarchy path hashed short and stable across runs, so two "Red Cube"s stay distinct.
        private string DerivedId()
        {
            string path = name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return $"{Sanitize(name)}-{(uint)path.GetHashCode():x8}";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "entity";

            char[] chars = value.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '-';
            }

            return new string(chars);
        }
    }
}
