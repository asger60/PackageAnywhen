using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

[Serializable]
public class AnySection
{
    public int rootNote;
    public AnywhenScaleObject scale;

    [Range(0, 1f)] public float volume = 0.85f;

    public List<AnySectionTrack> tracks;

    public void Init(List<AnySongTrack> songTracks)
    {
        volume = 1f;
        rootNote = 0;
        tracks = new List<AnySectionTrack>();

        foreach (var track in songTracks)
        {
            var newTrack = new AnySectionTrack();
            newTrack.Init(track);
            tracks.Add(newTrack);
        }
    }

    public void UpdateTracks(List<AnySongTrack> songTracks)
    {
        foreach (var songTrack in songTracks)
        {
        }
    }

    public bool HasSongTrack(AnySongTrack songTrack)
    {
        foreach (var track in tracks)
        {
            if (track.GetSongTrack() == songTrack) return true;
        }


        return false;
    }


    public void AddSongTrack(AnySongTrack songTrack)
    {
        var newTrack = new AnySectionTrack();
        newTrack.Init(songTrack);
        tracks.Add(newTrack);
    }

    public void RemoveSongTrack(AnySongTrack songTrack)
    {
        foreach (var sectionTrack in tracks)
        {
            if (sectionTrack.GetSongTrack() == songTrack)
            {
                tracks.Remove(sectionTrack);
                break;
            }
        }
    }
}