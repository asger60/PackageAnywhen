using Editor.AnySong;
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
        if (GUILayout.Button("Edit", GUILayout.Height(35), GUILayout.Width(300)))
        {
            AnysongEditorWindow.ShowModuleWindow();
            
        }
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

    }
}
