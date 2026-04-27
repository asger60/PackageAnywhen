using System;
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


        public List<AnysongSection> Sections;
        public List<AnysongTrackSettings> Tracks;


        public AnywhenSnapshot snapshotA = new();
        public AnywhenSnapshot snapshotB = new();


        public string author = "Floppy Club";
        public Action OnSongMidiChanged;
        public Action OnSongSettingsChanged;

        private void OnValidate()
        {
     
            OnSongSettingsChanged?.Invoke();
        }


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
                        for (var i = 0; i < pattern.steps.Count; i++)
                        {
                            var step = pattern.steps[i];
                            var patternStep = step;
                            patternStep.mixWeight = Random.Range(0, 1f);
                            pattern.steps[i] = patternStep;
                        }
                    }
                }
            }
        }

        [ContextMenu("Reset")]
        public void Reset()
        {
            Sections.Clear();
            foreach (var track in Tracks)
            {
                var newSection = new AnysongSection();
                newSection.Init(Tracks);
                Sections.Add(newSection);
            }

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


        public void SyncToClock()
        {
            foreach (var section in Sections)
            {
                section.SyncToClock();
            }
        }

        public void Refresh()
        {
            OnSongMidiChanged?.Invoke();
        }
    }
}