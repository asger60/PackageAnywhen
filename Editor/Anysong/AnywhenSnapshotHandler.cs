using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnywhenSnapshotHandler
{
// --- Snapshot storage ---
    private struct PropertyValue
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

    private List<PropertyValue> _snapshotA = new();
    private List<PropertyValue> _snapshotB = new();
    private float _blendT = 0f;

    // --- Capture ---
    private void CaptureSnapshot(SerializedObject serializedObject, ref List<PropertyValue> snapshot)
    {
        snapshot = new List<PropertyValue>();
        serializedObject.Update();

        var iterator = serializedObject.GetIterator();
        if (!iterator.NextVisible(true)) return;

        do
        {
            var pv = ReadProperty(iterator);
            if (pv.HasValue) snapshot.Add(pv.Value);
        } while (iterator.NextVisible(false));
    }

    private PropertyValue? ReadProperty(SerializedProperty prop)
    {
        var pv = new PropertyValue { path = prop.propertyPath, type = prop.propertyType };

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Float: pv.floatVal = prop.floatValue; break;
            case SerializedPropertyType.Integer: pv.intVal = prop.intValue; break;
            case SerializedPropertyType.Boolean: pv.boolVal = prop.boolValue; break;
            case SerializedPropertyType.String: pv.stringVal = prop.stringValue; break;
            case SerializedPropertyType.Color: pv.colorVal = prop.colorValue; break;
            case SerializedPropertyType.Vector2: pv.vec2Val = prop.vector2Value; break;
            case SerializedPropertyType.Vector3: pv.vec3Val = prop.vector3Value; break;
            case SerializedPropertyType.Vector4: pv.vec4Val = prop.vector4Value; break;
            case SerializedPropertyType.Quaternion: pv.quatVal = prop.quaternionValue; break;
            default: return null; // skip non-lerpable types (objects, arrays, etc.)
        }

        return pv;
    }

    // --- Blend / Apply ---
    private void ApplyBlend(SerializedObject serializedObject, float t)
    {
        // Build lookup for B
        var bLookup = new Dictionary<string, PropertyValue>(_snapshotB.Count);
        foreach (var pv in _snapshotB) bLookup[pv.path] = pv;

//        Undo.RecordObject(target, "Blend Snapshots");
        serializedObject.Update();

        foreach (var a in _snapshotA)
        {
            if (!bLookup.TryGetValue(a.path, out var b)) continue;
            var prop = serializedObject.FindProperty(a.path);
            if (prop == null) continue;

            WriteBlended(prop, a, b, t);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void WriteBlended(SerializedProperty prop, PropertyValue a, PropertyValue b, float t)
    {
        switch (a.type)
        {
            case SerializedPropertyType.Float:
                prop.floatValue = Mathf.Lerp(a.floatVal, b.floatVal, t);
                break;
            case SerializedPropertyType.Integer:
                prop.intValue = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = t >= 0.5f ? b.boolVal : a.boolVal;
                break;
            case SerializedPropertyType.Color:
                prop.colorValue = Color.Lerp(a.colorVal, b.colorVal, t);
                break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = Vector2.Lerp(a.vec2Val, b.vec2Val, t);
                break;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = Vector3.Lerp(a.vec3Val, b.vec3Val, t);
                break;
            case SerializedPropertyType.Vector4:
                prop.vector4Value = Vector4.Lerp(a.vec4Val, b.vec4Val, t);
                break;
            case SerializedPropertyType.Quaternion:
                prop.quaternionValue = Quaternion.Slerp(a.quatVal, b.quatVal, t);
                break;
            case SerializedPropertyType.String:
                prop.stringValue = t >= 0.5f ? b.stringVal : a.stringVal;
                break;
        }
    }
}