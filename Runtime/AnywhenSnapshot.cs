using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anywhen
{
    [Serializable]
    public class AnywhenSnapshot
    {
        public enum AnywhenPropertyType
        {
            Float,
            Integer,
            Boolean,
            String,
            Color,
            Vector2,
            Vector3,
            Vector4,
            Quaternion,
            Enum,
            AnimationCurve,
        }

        [Serializable]
        public struct PropertyValue
        {
            public string path;
            public AnywhenPropertyType type;
            public float floatVal;
            public int intVal;
            public bool boolVal;
            public Vector2 vec2Val;
            public Vector3 vec3Val;
            public Vector4 vec4Val;
            public Color colorVal;
            public Quaternion quatVal;
            public string stringVal;
            public AnimationCurve curveVal;
        }

        [SerializeField] private List<PropertyValue> _snapshot = new();
        public List<PropertyValue> Snapshot => _snapshot;

        public AnywhenSnapshot Clone()
        {
            var clone = new AnywhenSnapshot();
            foreach (var pv in _snapshot)
            {
                var newPv = pv;
                if (pv.curveVal != null)
                {
                    newPv.curveVal = new AnimationCurve(pv.curveVal.keys)
                    {
                        postWrapMode = pv.curveVal.postWrapMode,
                        preWrapMode = pv.curveVal.preWrapMode
                    };
                }
                clone.Snapshot.Add(newPv);
            }
            return clone;
        }
    }
}