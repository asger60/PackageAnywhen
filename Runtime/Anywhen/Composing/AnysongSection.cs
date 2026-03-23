using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongSection
    {
        [FormerlySerializedAs("patternSteps")] public AnywhenProgressionPatternObject.ProgressionStep[] progressionSteps;
        public List<AnysongSectionTrack> tracks;
        public int sectionLength = 4;


        public AnysongSectionTrack GetTrack(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            foreach (var track in tracks)
            {
                if (track.anysongTrackSettings.trackType == trackType) return track;
            }

            return null;
        }

        public void Init(List<AnysongTrackSettings> songTracks)
        {
            sectionLength = 4;
            tracks = new List<AnysongSectionTrack>();

            progressionSteps = new AnywhenProgressionPatternObject.ProgressionStep[4];
            for (var i = 0; i < progressionSteps.Length; i++)
            {
                progressionSteps[i] = new AnywhenProgressionPatternObject.ProgressionStep
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

        public void SetupTracks(List<AnysongTrackSettings> songTracks)
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                track.SetTrack(songTracks[i]);
            }
        }

        public void AddSongTrack(AnysongTrackSettings songTrackSettings)
        {
            var newTrack = new AnysongSectionTrack();
            newTrack.Init(songTrackSettings);
            tracks.Add(newTrack);
        }

        public void RemoveSongTrack(int trackIndex)
        {
            tracks.RemoveAt(trackIndex);
        }

        public AnywhenProgressionPatternObject.ProgressionStep GetProgressionStep(int currentBar, AnysongSection masterSection)
        {
            if (progressionSteps.Length != 0) return progressionSteps[(int)Mathf.Repeat(currentBar, progressionSteps.Length)];

            progressionSteps = new AnywhenProgressionPatternObject.ProgressionStep[masterSection.progressionSteps.Length];
            for (int i = 0; i < masterSection.progressionSteps.Length; i++)
            {
                progressionSteps[i] = new AnywhenProgressionPatternObject.ProgressionStep
                {
                    anywhenScale = masterSection.progressionSteps[i].anywhenScale,
                    rootNote = masterSection.progressionSteps[i].rootNote
                };
            }

            return progressionSteps[(int)Mathf.Repeat(currentBar, progressionSteps.Length)];
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