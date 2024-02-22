using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class AnyPatternStep
{
    public bool noteOn;
    public bool noteOff;
    public float duration;
    public float offset;
    public float velocity;

    [Range(0, 1f)] public float mixWeight;
    public bool isChord;
    public List<int> notes;

    public bool addNoteRandom;
    public int noteRandom;

    [Range(0, 1f)] public float chance = 1;
    [Range(0, 1f)] public float expression = 0;

    public int GetNote()
    {
        return notes.Count > 1 ? notes[Random.Range(0, notes.Count)] : notes[0];
    }

    public int[] GetNotes()
    {
        int[] r = new int[notes.Count];
        for (var i = 0; i < notes.Count; i++)
        {
            var note = notes[i];
            r[i] = note + Random.Range(-noteRandom, noteRandom);
        }

        return r;
    }


    public void Init()
    {
        duration = 1;
        expression = 1;
        velocity = 1;
        notes = new List<int> { 0 };
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
        clone.notes = new List<int>();
        foreach (var note in notes)
        {
            clone.notes.Add(note);
        }


        return clone;
    }

#if UNITY_EDITOR
    public void DrawInspector()
    {
        var step = this;
        step.noteOn = EditorGUILayout.Toggle("Note On", step.noteOn);
        step.noteOff = EditorGUILayout.Toggle("Note Off", step.noteOff);
        step.duration = EditorGUILayout.FloatField("Duration", step.duration);
        step.offset = EditorGUILayout.Slider("Nudge", step.offset, -1, 1);
        step.velocity = EditorGUILayout.Slider("Velocity", step.velocity, 0, 1);
        step.chance = EditorGUILayout.Slider("Chance", step.chance, 0, 1);
        //step.expression = EditorGUILayout.Slider("Expression", step.expression, 0, 1);
        //step.mixWeight = EditorGUILayout.FloatField("Weight", step.mixWeight);

        step.isChord = EditorGUILayout.Toggle("Is Chord", step.isChord);

        if (step.isChord)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Notes", GUILayout.Width(150));
            for (int i = 0; i < step.notes.Count; i++)
            {
                step.notes[i] = EditorGUILayout.IntField("", step.notes[i], GUILayout.Width(20));
            }


            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                step.notes.Add(new int());
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                step.notes.RemoveAt(step.notes.Count - 1);
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            step.notes[0] = EditorGUILayout.IntField("Note", step.notes[0]);
        }

        step.addNoteRandom = EditorGUILayout.Toggle("Add Note Random", step.addNoteRandom);
        if (step.addNoteRandom)
            step.noteRandom = EditorGUILayout.IntField("Note random", step.noteRandom);
    }
#endif
}