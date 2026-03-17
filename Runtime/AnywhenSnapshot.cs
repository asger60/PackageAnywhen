using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Anywhen
{
    [Serializable]
    public class AnywhenSnapshot
    {
        [Serializable]
        public struct PropertyValue
        {
            public string path;
            public SerializedPropertyType type;
            public float floatVal;
            public int intVal;
            public bool boolVal;
            public Vector2 vec2Val;
            public Vector3 vec3Val;
            public Vector4 vec4Val;
            public Color colorVal;
            public Quaternion quatVal;
            public string stringVal;
        }

        [SerializeField] private List<PropertyValue> _snapshot = new();
        public List<PropertyValue> Snapshot => _snapshot;
    }
}