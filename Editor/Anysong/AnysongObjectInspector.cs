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
        //GUILayout.FlexibleSpace();
        //GUILayout.Label("loading is now done from the antwhen song player");
        if (GUILayout.Button("Edit song"))
        {
            AnysongEditorWindow.ShowModuleWindow(target as AnysongObject);
            AnysongEditorWindow.LoadSong(target as AnysongObject);
        }

        //GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
}