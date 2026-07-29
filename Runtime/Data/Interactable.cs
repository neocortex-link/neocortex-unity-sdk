using System;
using Neocortex.Data;
using Newtonsoft.Json;

namespace Neocortex
{
    /// <summary>One thing a character can perceive: an object, another character, or the player.</summary>
    [Serializable]
    public class Interactable
    {
        /// <summary>Stable unique id. Actions reference entities by this, never by name.</summary>
        public string id;

        /// <summary>What this thing is and its state, as name/value pairs (type: door, color: red).</summary>
        public InteractableProperty[] properties;

        /// <summary>Set when this entity IS a Neocortex character.</summary>
        public string characterId;

        public string name;
        public Point3 position;

        /// <summary>Legacy CHARACTER/OBJECT split, derived SDK-side. Kept for v2 echo compat.</summary>
        public string type;

        public bool isSubject;

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
