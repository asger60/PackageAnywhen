using System;
using System.Collections.Generic;
using Anywhen;
using UnityEditor;
using UnityEngine;


public static class AnywhenSnapshotEditor
{
    // --- Capture ---
    public static void CaptureSnapshot(SerializedObject serializedObject, ref AnywhenSnapshot snapshot)
    {
        snapshot = new AnywhenSnapshot();
        serializedObject.Update();

        var iterator = serializedObject.GetIterator();
        if (!iterator.NextVisible(true)) return;

        do
        {
            if (iterator.name == "Tracks")
            {
                var tracksIterator = iterator.Copy();
                var endProp = tracksIterator.GetEndProperty();
                while (tracksIterator.NextVisible(true) && !SerializedProperty.EqualContents(tracksIterator, endProp))
                {
                    var pv = ReadProperty(tracksIterator);
                    if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);

                    if (tracksIterator.name == "trackFilters")
                    {
                        var filtersArray = tracksIterator.Copy();
                        for (int i = 0; i < filtersArray.arraySize; i++)
                        {
                            var element = filtersArray.GetArrayElementAtIndex(i);
                            if (element.objectReferenceValue != null)
                            {
                                var filterSO = new SerializedObject(element.objectReferenceValue);
                                var filterIterator = filterSO.GetIterator();
                                if (filterIterator.NextVisible(true))
                                {
                                    do
                                    {
                                        var fpv = ReadProperty(filterIterator);
                                        if (fpv.HasValue)
                                        {
                                            var prefixedPv = fpv.Value;
                                            prefixedPv.path = $"{NormalizePath(element.propertyPath)}.{fpv.Value.path}";
                                            snapshot.Snapshot.Add(prefixedPv);
                                        }
                                    } while (filterIterator.NextVisible(true));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var pv = ReadProperty(iterator);
                if (pv.HasValue) snapshot.Snapshot.Add(pv.Value);
            }
        } while (iterator.NextVisible(false));
    }

    private static string NormalizePath(string path)
    {
        // Unity property paths use .Array.data[i] for array elements.
        // We normalize this to just [i] to be more robust and independent of internal format.
        return path.Replace(".Array.data[", "[").Replace("Array.data[", "[");
    }

    private static string RestoreUnityPath(string path)
    {
        // Simple regex-like replacement for [i] to .Array.data[i]
        // This is primarily for SerializedObject.FindProperty in Editor.
        // We look for [ followed by digits followed by ]
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '[' && i > 0 && char.IsDigit(path[i + 1]))
            {
                sb.Append(".Array.data[");
            }
            else
            {
                sb.Append(path[i]);
            }
        }
        return sb.ToString();
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
            //case SerializedPropertyType.Enum: anywhenType = AnywhenSnapshot.AnywhenPropertyType.Enum; break;
            default: return null;
        }

        var pv = new AnywhenSnapshot.PropertyValue { path = NormalizePath(prop.propertyPath), type = anywhenType };

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
            //case SerializedPropertyType.Enum: pv.intVal = prop.intValue; break;
            default: return null;
        }

        return pv;
    }

    // --- Blend / Apply ---
    public static void ApplyBlend(AnywhenSnapshot snapshotA, AnywhenSnapshot snapshotB, SerializedObject serializedObject, float t)
    {
        var bLookup = new Dictionary<string, AnywhenSnapshot.PropertyValue>(snapshotB.Snapshot.Count);
        foreach (var pv in snapshotB.Snapshot) bLookup[pv.path] = pv;

        serializedObject.Update();

        var filtersToApply = new List<SerializedObject>();

        foreach (var a in snapshotA.Snapshot)
        {
            if (!bLookup.TryGetValue(a.path, out var b)) continue;

            SerializedProperty prop = null;
            if (a.path.Contains(".trackFilters["))
            {
                // Path like: Tracks[0].trackFilters[0].lowPassSettings.cutoffFrequency
                var parts = a.path.Split(new[] { ".trackFilters[" }, StringSplitOptions.None);
                var trackPath = RestoreUnityPath(parts[0]);
                var rest = parts[1]; // 0].lowPassSettings.cutoffFrequency

                var indexEnd = rest.IndexOf(']');
                var filterIndexStr = rest.Substring(0, indexEnd);
                var filterPropPath = rest.Substring(indexEnd + 2); // skip "]."

                var trackProp = serializedObject.FindProperty(trackPath);
                if (trackProp != null)
                {
                    var filtersProp = trackProp.FindPropertyRelative("trackFilters");
                    if (filtersProp != null && int.TryParse(filterIndexStr, out var filterIndex))
                    {
                        if (filterIndex < filtersProp.arraySize)
                        {
                            var filterObj = filtersProp.GetArrayElementAtIndex(filterIndex).objectReferenceValue;
                            if (filterObj != null)
                            {
                                var filterSO = filtersToApply.Find(so => so.targetObject == filterObj);
                                if (filterSO == null)
                                {
                                    filterSO = new SerializedObject(filterObj);
                                    filtersToApply.Add(filterSO);
                                }
                                prop = filterSO.FindProperty(filterPropPath);
                            }
                        }
                    }
                }
            }
            else
            {
                prop = serializedObject.FindProperty(RestoreUnityPath(a.path));
            }

            if (prop == null) continue;

            WriteBlended(prop, a, b, t);
        }

        serializedObject.ApplyModifiedProperties();
        foreach (var filterSO in filtersToApply)
        {
            filterSO.ApplyModifiedProperties();
        }
    }

    private static void WriteBlended(SerializedProperty prop, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
    {
        switch (a.type)
        {
            case AnywhenSnapshot.AnywhenPropertyType.Float:
                prop.floatValue = Mathf.Lerp(a.floatVal, b.floatVal, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Integer:
                prop.intValue = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Boolean:
                prop.boolValue = t >= 0.5f ? b.boolVal : a.boolVal;
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Color:
                prop.colorValue = Color.Lerp(a.colorVal, b.colorVal, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Vector2:
                prop.vector2Value = Vector2.Lerp(a.vec2Val, b.vec2Val, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Vector3:
                prop.vector3Value = Vector3.Lerp(a.vec3Val, b.vec3Val, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Vector4:
                prop.vector4Value = Vector4.Lerp(a.vec4Val, b.vec4Val, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.Quaternion:
                prop.quaternionValue = Quaternion.Slerp(a.quatVal, b.quatVal, t);
                break;
            case AnywhenSnapshot.AnywhenPropertyType.String:
                prop.stringValue = t >= 0.5f ? b.stringVal : a.stringVal;
                break;
            case AnywhenSnapshot.AnywhenPropertyType.AnimationCurve:
                prop.animationCurveValue = t >= 0.5f ? b.curveVal : a.curveVal;
                break;
        }
    }
}