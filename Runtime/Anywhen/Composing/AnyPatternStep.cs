using System;
using System.Collections.Generic;
using Anywhen;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


[Serializable]
public class AnyPatternStep
{
    public bool noteOn;
    public bool noteOff;
    [Range(0, 10f)] public float duration;
    [Range(-0.5f, 0.5f)] public float offset;
    [Range(0, 1f)] public float velocity;


    [Range(0, 1f)] public float mixWeight;
    public bool IsChord => chordNotes.Count > 0;


    public List<int> chordNotes = new List<int>();
    [Range(0, 1f)] public float strumAmount;

    [Range(0, 1f)] public float strumRandom;


    [Range(0, 4)] public int stepRepeats;

    [Range(0, 1f)] public float chance = 1;
    [Range(0, 1f)] public float expression = 0;


    public int rootNote;

    public enum RepeatRates
    {
        ThirtyTwo,
        FourtyEight,
        SixtyFour
    }

    public RepeatRates repeatRate;

    public int[] GetNotes(int patternRoot)
    {
        if (!IsChord)
            return new[] { rootNote + patternRoot };

        var chord = new int[chordNotes.Count + 1];
        chord[0] = rootNote + patternRoot;
        for (int i = 0; i < chordNotes.Count; i++)
        {
            chord[i + 1] = rootNote + chordNotes[i] + patternRoot;
        }

        return chord;
    }


    public void Init()
    {
        duration = 1;
        expression = 1;
        velocity = 1;
        chordNotes = new List<int> { };
        mixWeight = Random.Range(0, 1f);
    }


    public NoteEvent GetEvent(int patternRoot)
    {
        NoteEvent.EventTypes type = NoteEvent.EventTypes.NoteOn;
        if (noteOff)
            type = NoteEvent.EventTypes.NoteOff;

        var e = new NoteEvent(GetNotes(patternRoot), type, velocity,
            offset * AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub16),
            CreateStrum(GetNotes(patternRoot).Length), expression, 1)
        {
            duration = duration
        };
        return e;
    }

    public NoteEvent[] GetRepeats(int patternRoot, float trackVolume)
    {
        NoteEvent[] events = new NoteEvent[stepRepeats];

        for (int i = 0; i < stepRepeats; i++)
        {
            NoteEvent.EventTypes type = NoteEvent.EventTypes.NoteOn;
            if (noteOff)
                type = NoteEvent.EventTypes.NoteOff;

            var e = new NoteEvent(GetNotes(patternRoot), type, velocity * trackVolume,
                offset * AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub16) +
                (AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub16) / ((int)repeatRate + 2) *
                 (i + 1)),
                CreateStrum(GetNotes(patternRoot).Length), expression, 1)
            {
                duration = duration
            };
            events[i] = e;
        }


        return events;
    }

    double[] CreateStrum(int count)
    {
        if (count == 1)
        {
            return new double[] { 0 };
        }

        var notes = new double[count];
        var maxLength = AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub16);
        for (int i = 0; i < notes.Length; i++)
        {
            notes[i] = (maxLength * (strumAmount) * (float)i / (count - 1))
                       + maxLength * Random.Range(0, strumRandom);
        }

        return notes;
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