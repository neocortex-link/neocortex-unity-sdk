using System;
using Neocortex.Data;
using Newtonsoft.Json;

namespace Neocortex
{
    [Serializable]
    public struct Interactable
    {
        public string type;
        public string name;
        public bool isSubject;
        public Point3 position;
        public InteractableProperty[] properties;

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
