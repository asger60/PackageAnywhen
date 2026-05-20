using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;

public static class AnywhenSnapshotEditor
{
    // --- Capture ---
    public static void CaptureSnapshot(SerializedObject serializedObject, ref AnywhenSnapshot snapshot)
    {
        AnysongObject song = (AnysongObject)serializedObject.targetObject;
        bool isSnapshotB = (snapshot == song.snapshotB);

        snapshot = new AnywhenSnapshot();
        serializedObject.Update();

        var iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        if (iterator.NextVisible(true))
        {
            do
            {
                if (iterator.name == "m_Script" || iterator.name == "Tracks" || iterator.name == "Sections" || 
                    iterator.name == "snapshotA" || iterator.name == "snapshotB")
                {
                    enterChildren = false;
                    continue;
                }
                enterChildren = true;

                var pv = ReadProperty(iterator);
                if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);
            } while (iterator.NextVisible(enterChildren));
        }

        var tracksProp = serializedObject.FindProperty("Tracks");
        if (tracksProp != null)
        {
            for (int i = 0; i < song.Tracks.Count; i++)
            {
                var track = song.Tracks[i];
                var trackProp = tracksProp.GetArrayElementAtIndex(i);
                if (isSnapshotB) CaptureTrackSnapshot(trackProp, ref track.snapshotB);
                else CaptureTrackSnapshot(trackProp, ref track.snapshotA);
            }
        }

        EditorUtility.SetDirty(song);
    }

    private static void CaptureTrackSnapshot(SerializedProperty trackProp, ref AnywhenSnapshot snapshot)
    {
        snapshot = new AnywhenSnapshot();
        var iterator = trackProp.Copy();
        var end = trackProp.GetEndProperty();

        string trackPath = NormalizePath(trackProp.propertyPath);

        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            if (iterator.name == "m_Script" || iterator.name == "snapshotA" || iterator.name == "snapshotB")
            {
                enterChildren = false;
                continue;
            }
            enterChildren = true;

            var pv = ReadProperty(iterator);
            if (pv.HasValue)
            {
                var val = pv.Value;
                if (val.path.StartsWith(trackPath + "."))
                {
                    val.path = val.path.Substring(trackPath.Length + 1);
                }

                snapshot.Snapshot.Add(val);
            }
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