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

    public void TriggerStep(AnysongTrack track)
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

}