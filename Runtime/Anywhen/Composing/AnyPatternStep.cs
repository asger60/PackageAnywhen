using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEngine;


[Serializable]
public class AnyPatternStep
{
    public bool noteOn;
    public bool noteOff;
    [Range(0, 10f)] public float duration;
    [Range(-1, 1f)] public float offset;
    [Range(0, 1f)] public float velocity;


    [Range(0, 1f)] public float mixWeight;
    public bool IsChord => chordNotes.Count > 0;


    public List<int> chordNotes = new List<int>();


    [Range(0, 1f)] public float chance = 1;
    [Range(0, 1f)] public float expression = 0;


    public int rootNote;


    public int[] GetNotes()
    {
        if (!IsChord)
            return new[] { rootNote };

        var chord = new int[chordNotes.Count + 1];
        chord[0] = rootNote;
        for (int i = 0; i < chordNotes.Count; i++)
        {
            chord[i + 1] = rootNote + chordNotes[i];
        }

        return chord;
    }


    public void Init()
    {
        duration = 1;
        expression = 1;
        velocity = 1;
        chordNotes = new List<int> { };
        mixWeight = 0.5f;
    }

    public void TriggerStep(AnySongTrack track)
    {
        if (noteOn || noteOff)
            track.TriggerNoteOn(this, track.volume);
    }

    public NoteEvent GetEvent()
    {
        NoteEvent.EventTypes type = NoteEvent.EventTypes.NoteOn;
        if (noteOff)
            type = NoteEvent.EventTypes.NoteOff;

        var e = new NoteEvent(GetNotes(), type, velocity,
            offset, new double[GetNotes().Length], expression, 1)
        {
            duration = duration
        };
        return e;
    }


    public AnyPatternStep Clone()
    {
        var clone = (AnyPatternStep)MemberwiseClone();
        clone.chordNotes = new List<int>();
        foreach (var note in chordNotes)
        {
            clone.chordNotes.Add(note);
        }


        return clone;
    }

#if UNITY_EDITOR
    public void DrawInspector()
    {
        //var step = this;
        //step.noteOn = EditorGUILayout.Toggle("Note On", step.noteOn);
        //step.noteOff = EditorGUILayout.Toggle("Note Off", step.noteOff);
        //step.duration = EditorGUILayout.FloatField("Duration", step.duration);
        //step.offset = EditorGUILayout.Slider("Nudge", step.offset, -1, 1);
        //step.velocity = EditorGUILayout.Slider("Velocity", step.velocity, 0, 1);
        //step.chance = EditorGUILayout.Slider("Chance", step.chance, 0, 1);
//
        ////step.IsChord = EditorGUILayout.Toggle("Is Chord", step.IsChord);
//
        //if (step.IsChord)
        //{
        //    EditorGUILayout.BeginHorizontal();
        //    EditorGUILayout.LabelField("Notes", GUILayout.Width(150));
        //    for (int i = 0; i < step.notes.Count; i++)
        //    {
        //        step.notes[i] = EditorGUILayout.IntField("", step.notes[i], GUILayout.Width(20));
        //    }
//
//
        //    if (GUILayout.Button("+", GUILayout.Width(20)))
        //    {
        //        step.notes.Add(new int());
        //    }
//
        //    if (GUILayout.Button("-", GUILayout.Width(20)))
        //    {
        //        step.notes.RemoveAt(step.notes.Count - 1);
        //    }
//
        //    EditorGUILayout.EndHorizontal();
        //}
        //else
        //{
        //    step.notes[0] = EditorGUILayout.IntField("Note", step.notes[0]);
        //}
//
        //step.addNoteRandom = EditorGUILayout.Toggle("Add Note Random", step.addNoteRandom);
        //if (step.addNoteRandom)
        //    step.noteRandom = EditorGUILayout.IntField("Note random", step.noteRandom);
    }
#endif
}