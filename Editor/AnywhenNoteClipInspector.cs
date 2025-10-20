using Anywhen;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(AnywhenNoteClip))]
[CanEditMultipleObjects]
public class AnywhenNoteClipInspector : Editor
{
    private AudioClip _editorClip;
    private AnywhenNoteClip _target;
    private bool _noteDown;
    private bool _isPlaying;


    void OnEnable()
    {
        _target = (AnywhenNoteClip)target;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("Samples", _target.clipSamples.Length.ToString());


        if (GUILayout.Button("PLAY"))
        {
            AnywhenRuntime.PreviewNoteClip(_target);
        }

        if (GUILayout.Button("STOP"))
        {
            AnywhenRuntime.StopNoteClipPreview(_target);
        }
    }
}