using System;
using System.Collections.Generic;

using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongSection
    {
        
        public AnywhenProgressionPatternObject.ProgressionStep[] patternSteps;
        
        public List<AnysongSectionTrack> tracks;
        public int sectionLength = 4;
        
        [NonSerialized] private Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongSectionTrack> _trackDictionary;

        private void RebuildDictionary()
        {
            _trackDictionary = new Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongSectionTrack>();
            if (tracks == null) return;
            foreach (var track in tracks)
            {
                if (track.AnysongTrackSettings == null) continue;
                _trackDictionary.TryAdd(track.AnysongTrackSettings.trackType, track);
            }
        }

        public AnysongSectionTrack GetTrack(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            if (_trackDictionary == null) RebuildDictionary();
            return _trackDictionary.GetValueOrDefault(trackType);
        }

        public void Init(List<AnysongTrackSettings> songTracks)
        {
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

            RebuildDictionary();
        }

        public void SetupTracks(List<AnysongTrackSettings> songTracks)
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                track.SetTrack(songTracks[i]);
            }

            RebuildDictionary();
        }



        public void AddSongTrack(AnysongTrackSettings songTrackSettings)
        {
            var newTrack = new AnysongSectionTrack();
            newTrack.Init(songTrackSettings);
            tracks.Add(newTrack);
            RebuildDictionary();
        }

        public void RemoveSongTrack(int trackIndex)
        {
            tracks.RemoveAt(trackIndex);
            RebuildDictionary();
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

        public void SyncToClock()
        {
            foreach (var sectionTrack in tracks)
            {
                sectionTrack.SyncToClock();
            }
        }
    }
}