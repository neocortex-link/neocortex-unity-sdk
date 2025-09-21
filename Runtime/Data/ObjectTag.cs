using System;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

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

    [Serializable]
    public struct Quat
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator Quat(Quaternion q)
        {
            return new Quat() { x = q.x, y = q.y, z = q.z, w = q.w };
        }

        public static implicit operator Quaternion(Quat q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }
    }

    public struct ObjectTagData
    {
        public bool isSubject;
        public string tag;
        public Point3 position;
    }

    public class ObjectTag : MonoBehaviour
    {
        public string tag;
        public bool isSubject;

        public string GetObjectData()
        {
            return JsonConvert.SerializeObject(new
            {
                isSubject = isSubject,
                tag = tag,
                position = (Point3)transform.position,
            });
        }
    }
}