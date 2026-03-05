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
        EditorGUILayout.LabelField("Samples", _target.clipSamples.Length.ToString());
        EditorGUILayout.LabelField("Channels", _target.channels.ToString());

        _target.NoteIndex = EditorGUILayout.IntField("Note index", _target.NoteIndex);

        EditorGUI.BeginChangeCheck();
        AnywhenNoteClip.ClipType clipType = (AnywhenNoteClip.ClipType)EditorGUILayout.EnumPopup("Clip Type", _target.Type);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_target, "Change Clip Type");
            _target.Type = clipType;
            EditorUtility.SetDirty(_target);
        }

        if (_target.Type == AnywhenNoteClip.ClipType.Percussion)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Percussion Note Index", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < AnywhenSampleInstrument.MidiDrumMappings.Length; i++)
            {
                GUIStyle style = new GUIStyle(GUI.skin.button);
                if (AnywhenSampleInstrument.MidiDrumMappings[i].MidiNote == _target.NoteIndex)
                {
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.cadetBlue;
                }

                if (GUILayout.Button(AnywhenSampleInstrument.MidiDrumMappings[i].Name, style))
                {
                    Undo.RecordObject(_target, "Set Note Index");
                    _target.NoteIndex = AnywhenSampleInstrument.MidiDrumMappings[i].MidiNote;
                    EditorUtility.SetDirty(_target);
                }
            }

            EditorGUILayout.EndHorizontal();
        }


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