using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;

public static class AnywhenSnapshotEditor
{
    // --- Capture ---
    public static void CaptureSnapshot(SerializedObject serializedObject, ref AnywhenSnapshot snapshot)
    {
        snapshot = new AnywhenSnapshot();
        serializedObject.Update();

        // Capture root properties
        var tempoProp = serializedObject.FindProperty("tempo");
        if (tempoProp != null)
        {
            var pv = ReadProperty(tempoProp);
            if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);
        }

        var volumeProp = serializedObject.FindProperty("songVolume");
        if (volumeProp != null)
        {
            var pv = ReadProperty(volumeProp);
            if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);
        }

        var tracksProp = serializedObject.FindProperty("Tracks");
        if (tracksProp == null) return;

        var iterator = tracksProp.Copy();
        var end = tracksProp.GetEndProperty();

        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
        {
            var pv = ReadProperty(iterator);
            if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace(".Array.data[", "[").Replace("Array.data[", "[");
    }

    private static AnywhenSnapshot.PropertyValue? ReadProperty(SerializedProperty prop)
    {
        AnywhenSnapshot.AnywhenPropertyType anywhenType;
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Float: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Float; break;
            case SerializedPropertyType.Integer: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Integer; break;
            case SerializedPropertyType.Boolean: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Boolean; break;
            case SerializedPropertyType.String: anywhenType = AnywhenSnapshot.AnywhenPropertyType.String; break;
            case SerializedPropertyType.Color: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Color; break;
            case SerializedPropertyType.Vector2: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Vector2; break;
            case SerializedPropertyType.Vector3: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Vector3; break;
            case SerializedPropertyType.Vector4: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Vector4; break;
            case SerializedPropertyType.Quaternion: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Quaternion; break;
            case SerializedPropertyType.AnimationCurve: anywhenType = AnywhenSnapshot.AnywhenPropertyType.AnimationCurve; break;
            case SerializedPropertyType.Enum: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Enum; break;
            default: return null;
        }

        var pv = new AnywhenSnapshot.PropertyValue
        {
            path = NormalizePath(prop.propertyPath),
            type = anywhenType
        };

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
            case SerializedPropertyType.AnimationCurve: pv.curveVal = prop.animationCurveValue; break;
            case SerializedPropertyType.Enum: pv.intVal = prop.enumValueIndex; break;
            default: return null;
        }

        return pv;
    }
}