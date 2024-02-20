using Anywhen.Composing;
using UnityEditor;
using UnityEngine;

namespace Editor.AnySong
{
    public static class AnyPatternEditor 
    {
        public static void DrawInspector(AnyPattern pattern)
        {
            
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
    }
}