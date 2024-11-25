using Anywhen;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(AnywhenNoteClip))]
[CanEditMultipleObjects]
public class AnywhenNoteClipInspector : UnityEditor.Editor
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

        //if (_target.loopSettings.enabled)
        //{
        //    _noteDown = GUILayout.Toggle(_noteDown, _noteDown ? "STOP" : "PLAY", "Button");
        //    if (_noteDown && !_isPlaying)
        //    {
        //        //AnywhenRuntime.ClipNoteClipPreviewer.PlayClip(_target);
        //        _isPlaying = true;
        //    }
//
        //    if (!_noteDown)
        //    {
        //        AnywhenRuntime.ClipNoteClipPreviewer.StopClip();
        //        _isPlaying = false;
        //    }
        //}
        //else
        {
            if (GUILayout.Button("PLAY"))
            {
                //AnywhenRuntime.ClipNoteClipPreviewer.PlayClip( _target);
            }
        }
    }
}