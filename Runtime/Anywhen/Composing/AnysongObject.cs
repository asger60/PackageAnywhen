using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "Anysong", menuName = "Anywhen/Anysong", order = 1)]
    public class AnysongObject : ScriptableObject
    {
        [Range(10, 200)] public int tempo;
        [Range(0, 1f)] public float songVolume = 1;

        public enum SongPlayModes
        {
            Edit,
            Playback,
        }

        private SongPlayModes _currentPlayMode = SongPlayModes.Edit;
        public SongPlayModes CurrentPlayMode => _currentPlayMode;

        int _currentEditSectionIndex = 0;
        public int CurrentEditSectionIndex => _currentEditSectionIndex;
        public List<AnysongSection> Sections;
        public List<AnysongTrackSettings> Tracks;


        [FormerlySerializedAs("SnapshotA")] public AnywhenSnapshot snapshotA = new();
        [FormerlySerializedAs("SnapshotB")] public AnywhenSnapshot snapshotB = new();


        public string author = "Floppy Club";


        public void Rebuild()
        {
            foreach (var section in Sections)
            {
                section.SetupTracks(Tracks);
            }
        }

        [ContextMenu("ClearPatterns")]
        void ClearPatterns()
        {
            Debug.Log("ClearPatterns");
            foreach (var section in Sections)
            {
                section.Init(Tracks);
            }
        }


        [ContextMenu("RandomizeStepWeights")]
        void RandomizeStepWeights()
        {
            foreach (var anySection in Sections)
            {
                foreach (var rSectionTrack in anySection.tracks)
                {
                    foreach (var pattern in rSectionTrack.patterns)
                    {
                        foreach (var step in pattern.steps)
                        {
                            step.mixWeight = Random.Range(0, 1f);
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            foreach (var section in Sections)
            {
                section.Reset();
            }
        }

        public void UnMuteAll()
        {
            foreach (var track in Tracks)
            {
                track.UnMute();
            }
        }

        public void SetEditSection(int sectionIndex)
        {
            _currentEditSectionIndex = sectionIndex;
        }


        public void SyncToClock()
        {
            foreach (var section in Sections)
            {
                section.SyncToClock();
            }
        }
    }
}