using System;
using Neocortex.Data;
using Newtonsoft.Json;

namespace Neocortex
{
    /// <summary>
    ///     One thing the character can perceive: an object, another character, or the player.
    ///     Sent as world facts — position and state — with no interpretation attached; working out
    ///     what is near or worth acting on is the character's job.
    /// </summary>
    [Serializable]
    public class Interactable
    {
        /// <summary>Stable unique identifier. Actions reference entities by this, never by name.</summary>
        public string id;

        /// <summary>
        ///     Everything descriptive about this thing as name/value pairs — both what it IS
        ///     (type: door, kind: character) and its current state (color: red, locked: true). The
        ///     character reads meaning from these.
        /// </summary>
        public InteractableProperty[] properties;

        /// <summary>Set when this entity IS a Neocortex character, so it can recognise itself in the world.</summary>
        public string characterId;

        public string name;
        public Point3 position;

        /// <summary>Legacy CHARACTER/OBJECT split, derived SDK-side. Kept only for v2 echo compat.</summary>
        public string type;

        public bool isSubject;

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
