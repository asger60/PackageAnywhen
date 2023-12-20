using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class AnyPattern
{
    public int[] triggerBars;
    [Range(0, 1f)] public float triggerChance;
    public List<AnyPatternStep> steps;
    public bool isActive;

    public void Init()
    {
        triggerBars = new[] { 0 };
        triggerChance = 1;
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

        clone.triggerChance = triggerChance;
        return clone;
    }

    public bool GetIsActive(int currentBar)
    {
        currentBar = (int)Mathf.Repeat(currentBar, triggerBars.Length);

        foreach (var triggerBar in triggerBars)
        {
            if (triggerBar == currentBar) return true;
        }

        return false;
    }

#if UNITY_EDITOR

    public void DrawInspector()
    {
        var pattern = this;
        pattern.triggerChance = EditorGUILayout.FloatField("Trigger chance", pattern.triggerChance);

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