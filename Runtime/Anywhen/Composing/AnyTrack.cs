using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;

[Serializable]
public class AnyTrack
{
    [Range(0, 1f)] public float volume;
    public AnywhenInstrument instrument;
    public List<AnyPattern> patterns;

    private NoteEvent _lastTrackNote;

    public void Init()
    {
        volume = 1;
        patterns = new List<AnyPattern> { new AnyPattern() };
        foreach (var pattern in patterns)
        {
            pattern.Init();
        }
    }

    public AnyTrack Clone()
    {
        var clone = new AnyTrack
        {
            patterns = new List<AnyPattern>()
        };
        for (var i = 0; i < 16; i++)
        {
            clone.patterns.Add(patterns[i].Clone());
        }

        clone.volume = volume;

        return clone;
    }

    public void TriggerNoteOn(AnyPatternStep anyPatternStep)
    {
        _lastTrackNote = new NoteEvent(NoteEvent.EventTypes.NoteOn, anyPatternStep.offset,
            anyPatternStep.GetNotes(),
            new double[] { 0, 0, 0 }, anyPatternStep.expression, 1,
            anyPatternStep.velocity * instrument.volume)
        {
            duration = anyPatternStep.duration
        };


        AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackNote, instrument);
    }
    
#if UNITY_EDITOR

    public void DrawInspector()
    {
        var track = this;
        track.instrument = (AnywhenInstrument)EditorGUILayout.ObjectField("Instrument", track.instrument,
            typeof(AnywhenInstrument));
        track.volume = EditorGUILayout.FloatField("Volume", track.volume);
    }
#endif
}

