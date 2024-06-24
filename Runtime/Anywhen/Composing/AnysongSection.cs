using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEngine;

[Serializable]
public class AnysongSection
{
    public int rootNote;


    public AnywhenProgressionPatternObject.ProgressionStep[] patternSteps;

    [Range(0, 1f)] public float volume = 0.85f;

    public List<AnysongSectionTrack> tracks;
    public int sectionLength = 4;

    public void Init(List<AnysongTrack> songTracks)
    {
        volume = 1f;
        rootNote = 0;
        tracks = new List<AnysongSectionTrack>();

        patternSteps = new AnywhenProgressionPatternObject.ProgressionStep[4];
        for (var i = 0; i < patternSteps.Length; i++)
        {
            patternSteps[i] = new AnywhenProgressionPatternObject.ProgressionStep
            {
                rootNote = 0,
                anywhenScale = AnywhenConductor.GetDefaultScale()
            };
        }

        foreach (var track in songTracks)
        {
            var newTrack = new AnysongSectionTrack();
            newTrack.Init(track);
            tracks.Add(newTrack);
        }
        
    }

    public void UpdateTracks(List<AnysongTrack> songTracks)
    {
        foreach (var songTrack in songTracks)
        {
        }
    }


    public void AddSongTrack(AnysongTrack songTrack)
    {
        var newTrack = new AnysongSectionTrack();
        newTrack.Init(songTrack);
        tracks.Add(newTrack);
    }

    public void RemoveSongTrack(int trackIndex)
    {
        tracks.RemoveAt(trackIndex);
    }

    public AnywhenProgressionPatternObject.ProgressionStep GetProgressionStep(int currentBar)
    {
        return patternSteps[(int)Mathf.Repeat(currentBar, patternSteps.Length)];
    }
}