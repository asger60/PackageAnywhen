using System;
using System.Collections.Generic;

using UnityEngine;

namespace Anywhen.Composing
{
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
            sectionLength = 4;
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

        public AnywhenProgressionPatternObject.ProgressionStep GetProgressionStep(int currentBar, AnysongSection masterSection)
        {
            if (patternSteps.Length == 0)
            {
                patternSteps = new AnywhenProgressionPatternObject.ProgressionStep[masterSection.patternSteps.Length];
                for (int i = 0; i < masterSection.patternSteps.Length; i++)
                {
                    patternSteps[i] = new AnywhenProgressionPatternObject.ProgressionStep
                    {
                        anywhenScale = masterSection.patternSteps[i].anywhenScale,
                        rootNote = masterSection.patternSteps[i].rootNote
                    };
                }
                
            }
            return patternSteps[(int)Mathf.Repeat(currentBar, patternSteps.Length)];
        }
    
        public AnysongSection Clone()
        {
            var clone = new AnysongSection
            {
                tracks = new List<AnysongSectionTrack>()
            };
        
            for (var i = 0; i < tracks.Count; i++)
            {
                clone.tracks.Add(tracks[i].Clone());
            }


            return clone;
        }


        public void Reset()
        {
            foreach (var track in tracks)
            {
                track.Reset();
            }
        }
    }
}