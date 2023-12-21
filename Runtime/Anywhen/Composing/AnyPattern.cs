using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public class AnyPattern
{
    public List<float> triggerChances = new List<float>();
    [HideInInspector] public List<AnyPatternStep> steps;

    public void Init()
    {
        triggerChances.Add(0);

        steps = new List<AnyPatternStep>();
        for (int i = 0; i < 16; i++)
        {
            var newStep = new AnyPatternStep();
            newStep.Init();
            steps.Add(newStep);
        }
    }

    public AnyPattern Clone()
    {
        var clone = new AnyPattern
        {
            steps = new List<AnyPatternStep>()
        };
        for (var i = 0; i < 16; i++)
        {
            clone.steps.Add(steps[i].Clone());
        }

        clone.triggerChances.AddRange(triggerChances);
        return clone;
    }

    public bool TriggerOnBar(int currentBar)
    {
        currentBar = (int)Mathf.Repeat(currentBar, triggerChances.Count);

        return triggerChances[currentBar] > 0.5f;
    }

#if UNITY_EDITOR

    public void DrawInspector()
    {
        var pattern = this;
        GUILayout.BeginHorizontal();
        for (int i = 0; i < pattern.triggerChances.Count; i++)
        {
            pattern.triggerChances[i] = EditorGUILayout.FloatField(pattern.triggerChances[i]);
        }

        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            pattern.triggerChances.Add(new int());
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            pattern.triggerChances.RemoveAt(pattern.triggerChances.Count - 1);
        }

        GUILayout.EndHorizontal();

        //pattern.triggerChance = EditorGUILayout.FloatField("Trigger chance", pattern.triggerChance);


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy", GUILayout.Width(60)))
        {
            AnysongEditorWindow.CopyCurrentPattern();
        }

        if (AnysongEditorWindow.PatternCopy != null)
        {
            if (GUILayout.Button("Paste", GUILayout.Width(60)))
            {
                AnysongEditorWindow.PastePattern();
            }
        }

        GUI.color = Color.red;
        GUILayout.Space(20);
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            AnysongEditorWindow.ClearCurrentPattern();
        }

        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }
#endif
}