using System;
using Anywhen;
using UnityEditor;
using UnityEngine;


[Serializable]
public class AnySongTrack
{
    [Range(0, 1f)] public float volume;
    public AnywhenInstrument instrument;
    private NoteEvent _lastTrackNote;


    public void Init()
    {
        volume = 1;
    }

    public AnySongTrack Clone()
    {
        var clone = new AnySongTrack
        {
            instrument = instrument,
            volume = volume
        };

        return clone;
    }

    public void TriggerNoteOn(AnyPatternStep anyPatternStep)
    {
        
        //_lastTrackNote = new NoteEvent(anyPatternStep.GetNotes(), NoteEvent.EventTypes.NoteOn, anyPatternStep.velocity,
        //    anyPatternStep.offset, new double[anyPatternStep.GetNotes().Length], anyPatternStep.expression , 1)
        //{
        //    duration = anyPatternStep.duration
        //};

        _lastTrackNote = anyPatternStep.GetEvent();

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