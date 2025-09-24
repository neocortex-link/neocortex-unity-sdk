using UnityEngine;
using Newtonsoft.Json;

namespace Neocortex.Data
{
    public struct Point3
    {
        public float x;
        public float y;
        public float z;

        public static implicit operator Point3(Vector3 v)
        {
            return new Point3() { x = v.x, y = v.y, z = v.z };
        }

        public static implicit operator Vector3(Point3 p)
        {
            return new Vector3() { x = p.x, y = p.y, z = p.z };
        }

        public static implicit operator Point3(string s)
        {
            Point3 p = JsonConvert.DeserializeObject<Point3>(s);
            return p;
        }
    }
}
