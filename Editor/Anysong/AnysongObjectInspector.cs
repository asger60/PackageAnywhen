using Anywhen.Composing;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnysongObject))]
public class AnysongObjectInspector : UnityEditor.Editor
{

    public override void OnInspectorGUI()
    {
        
        base.OnInspectorGUI();
        
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("loading is now done from the antwhen song player");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

    }
}
