using System;
using UnityEngine;
using Neocortex.Data;

namespace Neocortex
{
    /// <summary>
    ///     Makes this GameObject part of what characters can perceive. Give it properties and every
    ///     message a nearby character sends carries it automatically, no code.
    ///
    ///     Properties are name/value pairs describing the thing: its "name" (seeded from the
    ///     GameObject when you add the component, and editable or removable), what it IS (type: door)
    ///     and its current state (color: red, locked: true). Characters reference entities by
    ///     <see cref="Id"/>, a short stable hash, never by name, so two "Red Cube"s stay
    ///     distinguishable.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Interactable", 0)]
    public class NeocortexInteractable : MonoBehaviour
    {
        [Tooltip("Unique id characters use to reference this. Leave empty to derive a stable one from the scene path.")]
        [SerializeField] private string id;

        [Tooltip("Everything about this thing as name/value pairs: name, type=door, color=red, locked=true. 'name' is seeded from the GameObject, edit it, add more, or remove any.")]
        [SerializeField] private InteractableProperty[] properties = Array.Empty<InteractableProperty>();

        /// <summary>Stable identity: the authored id, or one derived from this object's scene path.</summary>
        public string Id => string.IsNullOrEmpty(id) ? DerivedId() : id;

        public InteractableProperty[] Properties => properties;

        /// <summary>The "name" property's value, or "" if the developer removed it.</summary>
        public string Name => GetProperty("name");

        private NeocortexSmartAgent agent;

        // Runs when the component is first added in the editor: seed a "name" property from the
        // GameObject's name so there's a sensible default the developer can edit or remove.
        private void Reset()
        {
            properties = new[] { new InteractableProperty { name = "name", value = gameObject.name } };
        }

        private void Awake()
        {
            // An Interactable sitting on a character's GameObject IS that character: linking it
            // lets the character recognise itself in the world it perceives, and lets other
            // characters see it as a character rather than a prop.
            agent = GetComponent<NeocortexSmartAgent>();
        }

        public Interactable ToInteractable()
        {
            bool isCharacter = agent != null;

            return new Interactable
            {
                id = Id,
                properties = properties,
                characterId = agent != null ? agent.CharacterID : null,
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

        // Hierarchy path hashed to something short and stable across runs, so a character can
        // refer to the same object in a later turn and still be understood.
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
