using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongSection
    {
        public AnywhenProgressionPatternObject.ProgressionStep[] progressionSteps;
        public List<AnysongSectionTrack> tracks;
        public int sectionLength;


        public struct Unmanaged
        {
            public int SectionLength;
            public NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> ProgressionSteps;
            public NativeArray<AnysongSectionTrack.Unmanaged> Tracks;
            int _currentBar;

            public void AdvancePlayingSection()
            {
                _currentBar++;
            }

            public bool IsComplete()
            {
                return _currentBar >= SectionLength;
            }

            public void Reset()
            {
                _currentBar = 0;
            }
        }

        public Unmanaged ToUnmanaged()
        {
            var unmanagedPatterns = new AnywhenProgressionPatternObject.ProgressionStep.Unmanaged[progressionSteps.Length];
            for (int i = 0; i < progressionSteps.Length; i++)
                unmanagedPatterns[i] = progressionSteps[i].ToUnmanaged();

            var unmanagedTracks = new AnysongSectionTrack.Unmanaged[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                unmanagedTracks[i] = tracks[i].ToUnmanaged();

            return new Unmanaged
            {
                SectionLength = sectionLength,
                ProgressionSteps =
                    new NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged>(unmanagedPatterns,
                        Allocator.Persistent),
                Tracks = new NativeArray<AnysongSectionTrack.Unmanaged>(unmanagedTracks, Allocator.Persistent)
            };
        }

        public AnysongSectionTrack GetTrack(int trackType)
        {
            Debug.LogWarning("GetTrack() is not implemented yet");
            foreach (var track in tracks)
            {
                //  if (track.anysongTrackSettings.trackTypeIndex == trackType) return track;
            }

            return tracks[0];
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

            clone.progressionSteps = new AnywhenProgressionPatternObject.ProgressionStep[progressionSteps.Length];
            for (var i = 0; i < progressionSteps.Length; i++)
            {
                clone.progressionSteps[i] = progressionSteps[i].Clone();
            }

            clone.sectionLength = sectionLength;

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

        public bool IsNull()
        {
            return (tracks.Count == 0 && sectionLength == 0);
        }
    }
}