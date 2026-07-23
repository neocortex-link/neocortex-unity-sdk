using UnityEngine;
using Neocortex.Data;
using Newtonsoft.Json;

namespace Neocortex
{
    public class NeocortexInteractable : MonoBehaviour
    {
        public InteractableType Type;
        
        [Tooltip("The name of the interactable. This name will be used to identify the interactable object.")]
        public string Name;
        
        [Tooltip("Properties of the interactable object")]
        public InteractableProperty[] Properties;

        public Interactable ToInteractable()
        {
            return new Interactable
            {
                type = Type.ToString(),
                name = Name,
                position = transform.position,
                // Wire contract requires the field; subject-marking was never implemented.
                isSubject = false,
                properties = Properties
            };
        }
    }
}
