using System;
using System.Collections.Generic;
using Anywhen.SettingsObjects;
using UnityEngine;

[Serializable]
public class AnySection
{
    public int rootNote;
    public AnywhenScaleObject scale;

    [Range(0, 1f)] public float volume = 0.85f;

    public List<AnyTrack> tracks;

    public void Init()
    {
        volume = 1f;
        rootNote = 0;
        tracks = new List<AnyTrack> { new AnyTrack() };
        foreach (var track in tracks)
        {
            track.Init();
        }
    }
    
}