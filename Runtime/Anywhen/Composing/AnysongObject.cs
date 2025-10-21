using System;
using System.Collections.Generic;
using UnityEngine;
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
        public List<AnysongTrack> Tracks;
        private bool _sectionEditLock;
        public bool SectionEditLock => _sectionEditLock;
        private int _currentPlaybackSectionIndex = 0;
        public int CurrentSectionIndex
        {
            get => Mathf.Min(Mathf.Max(_currentPlaybackSectionIndex, 0), Sections.Count - 1);
            set => _currentPlaybackSectionIndex = value;
        }

        public string author = "Floppy Club";

        [ContextMenu("Init")]
        void Init()
        {
            foreach (var section in Sections)
            {
                section.Init(Tracks);
            }
        }

        public void Play(SongPlayModes playMode)
        {
            _currentPlayMode = playMode;
            switch (playMode)
            {
                case SongPlayModes.Edit:
                    _currentPlaybackSectionIndex = CurrentEditSectionIndex;
                    break;
                case SongPlayModes.Playback:
                    _currentPlaybackSectionIndex = 0;
                    Reset();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playMode), playMode, null);
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
            foreach (var track in Tracks)
            {
                track.Reset();
            }

            foreach (var section in Sections)
            {
                section.Reset();
            }
        }

        public void SetEditSection(int sectionIndex)
        {
            _currentEditSectionIndex = sectionIndex;
        }
        public void SetEditSectionLock(bool value)
        {
            _sectionEditLock = value;
        }
        
        public void AdvanceSection()
        {
            if (CurrentPlayMode == SongPlayModes.Edit && SectionEditLock)
            {
                _currentPlaybackSectionIndex = CurrentEditSectionIndex;
            }
            else
            {
                _currentPlaybackSectionIndex++;
                _currentPlaybackSectionIndex = (int)Mathf.Repeat(_currentPlaybackSectionIndex, Sections.Count);
            }
            

            //SetSection(_currentSectionIndex);
        }
    }
}