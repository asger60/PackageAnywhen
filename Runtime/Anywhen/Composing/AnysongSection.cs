using System;
using System.Collections.Generic;
using Anywhen.SettingsObjects;
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
        [SerializeField] [Range(0, 1f)] private float[] groove;

        public struct Unmanaged
        {
            public int SectionLength;
            public NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> ProgressionSteps;
            public NativeArray<AnysongSectionTrack.Unmanaged> Tracks;
            public NativeArray<float> Groove;

            public void Reset()
            {
                foreach (var track in Tracks)
                {
                    track.Reset();
                }
            }

            public void Dispose()
            {
                if (ProgressionSteps.IsCreated)
                {
                    for (int i = 0; i < ProgressionSteps.Length; i++)
                    {
                        if (ProgressionSteps[i].AnywhenScale.notes.IsCreated)
                        {
                            ProgressionSteps[i].AnywhenScale.notes.Dispose();
                        }
                    }

                    ProgressionSteps.Dispose();
                }

                if (Tracks.IsCreated)
                {
                    for (int i = 0; i < Tracks.Length; i++)
                    {
                        Tracks[i].Dispose();
                    }

                    Tracks.Dispose();
                }

                if (Groove.IsCreated) Groove.Dispose();
            }

            public float GetGrooveValue(int currentSub16Count)
            {
                if (!Groove.IsCreated || Groove.Length == 0) return 0;
                return Groove[currentSub16Count % Groove.Length];
            }
        }

        public Unmanaged ToUnmanaged()
        {
            var unmanagedProgression =
                new NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged>(progressionSteps.Length,
                    Allocator.Persistent);

            for (int i = 0; i < progressionSteps.Length; i++)
            {
                unmanagedProgression[i] = progressionSteps[i].ToUnmanaged();
            }

            var unmanagedTracks = new AnysongSectionTrack.Unmanaged[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                unmanagedTracks[i] = tracks[i].ToUnmanaged();

            return new Unmanaged
            {
                SectionLength = sectionLength,
                ProgressionSteps = unmanagedProgression,
                Tracks = new NativeArray<AnysongSectionTrack.Unmanaged>(unmanagedTracks, Allocator.Persistent),
                Groove = new NativeArray<float>(groove, Allocator.Persistent)
            };
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
                    anywhenScale = Resources.Load<AnywhenScaleObject>("Scales/Major")
                };
            }

            foreach (var track in songTracks)
            {
                var newTrack = new AnysongSectionTrack();
                newTrack.Init(track);
                tracks.Add(newTrack);
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


        public bool IsNull()
        {
            return (tracks.Count == 0 && sectionLength == 0);
        }
    }
}