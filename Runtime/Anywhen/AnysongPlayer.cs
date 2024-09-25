using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen
{
    public class AnysongPlayer : MonoBehaviour
    {
        [SerializeField] private AnysongObject songObject;
        public AnysongObject AnysongObject => songObject;
        private AnywhenInstrument[] _instruments;
        private AnysongObject _currentSong;
        private bool _isRunning;
        private bool _loaded = false;
        public bool IsSongLoaded => _loaded;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        int _currentSectionIndex = 0;
        public int CurrentSectionIndex => _currentSectionIndex;
        public bool IsPreviewing => _isPreviewing;

        [SerializeField] private AnywhenTrigger trigger;
        private bool _isPreviewing;
        private int _currentSongIndex;
        public int CurrentSongIndex => _currentSongIndex;

        private int _currentSongPackIndex;
        public int CurrentSongPackIndex => _currentSongPackIndex;

        private void Start()
        {
            Load(songObject);
            trigger.OnTrigger += Play;
        }

        public void Load()
        {
            Load(songObject);
        }

        private void Load(AnysongObject anysong)
        {
            _loaded = true;
            _currentSong = anysong;

            foreach (var track in anysong.Tracks)
            {
                if (track.instrument is AnywhenSynthPreset preset)
                {
                    AnywhenRuntime.AnywhenSynthHandler.RegisterPreset(preset);
                }
            }
        }

        private NoteEvent[] _lastTrackNote;
        private int _currentBar;

        private void OnBar()
        {
            if (!_isRunning) return;
            _currentBar++;
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                _currentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }

            var thisSection = _currentSong.Sections[_currentSectionIndex];
            int progress = (int)Mathf.Repeat(_currentBar, thisSection.sectionLength);

            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                track.AdvancePlayingPattern();
            }

            if (progress == 0)
            {
                NextSection();
            }

            
            var section = _currentSong.Sections[_currentSectionIndex];

            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(_currentBar, _currentSong.Sections[0]));
        }

        public int[] GetPlayingTrackPatternIndexes()
        {
            List<int> returnList = new List<int>();
            for (var i = 0; i < _currentSong.Tracks.Count; i++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[i];

                returnList.Add(track.GetPlayingPatternIndex());
            }

            return returnList.ToArray();
        }

        private void OnTick16()
        {
            if (!_isRunning) return;

            if (_currentSong != songObject)
            {
                Release();
                Load(songObject);
            }

            _currentSectionIndex = Mathf.Min(_currentSectionIndex, _currentSong.Sections.Count - 1);

            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];

                var pattern = track.GetPlayingPattern();
                var step = pattern.GetCurrentStep();
                pattern.Advance();


                if (track.isMuted) continue;


                var songTrack = _currentSong.Tracks[trackIndex];


                float thisIntensity =
                    Mathf.Clamp01(track.intensityMappingCurve.Evaluate(AnysongPlayerBrain.GlobalIntensity));
                float thisRnd = Random.Range(0, 1f);

                if (thisRnd < step.chance && step.mixWeight < thisIntensity)
                {
                    if (step.noteOn || step.noteOff)
                        songTrack.TriggerNoteOn(step, pattern);
                }
            }
        }

        void NextSection()
        {
            _currentSong.Reset();
            _currentBar = 0;
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                _currentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }
            else
            {
                _currentSectionIndex++;
                _currentSectionIndex = (int)Mathf.Repeat(_currentSectionIndex, _currentSong.Sections.Count);
                for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
                {
                    var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                    track.Reset();
                }
            }
        }


        private void Release()
        {
            _loaded = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }


        public void ReleaseFromMetronome()
        {
            _isRunning = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            AnywhenRuntime.Metronome.OnNextBar -= OnBar;
        }

        public void AttachToMetronome()
        {
            if (_isRunning) return;
            if (!_loaded) return;
            _isRunning = true;
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.Metronome.OnNextBar += OnBar;
            _currentSong.Reset();
        }

        public void Play()
        {
            _currentSong.Reset();
            _currentSectionIndex = 0;
            _currentBar = 0;
            var section = _currentSong.Sections[_currentSectionIndex];
            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(_currentBar, _currentSong.Sections[0]));

            AnysongPlayerBrain.TransitionTo(this, triggerTransitionsType);
        }

        public float GetTrackProgress()
        {
            int trackLength = _currentSong.Sections[_currentSectionIndex].patternSteps.Length;
            int progress = (int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, trackLength);
            return (float)progress / trackLength;
        }

        public Action<bool> OnPlay;

        public void ToggleEditorPreview()
        {
            _isPreviewing = !_isPreviewing;

            AnywhenRuntime.SetPreviewMode(_isPreviewing, this);
            OnPlay?.Invoke(_isPreviewing);
        }


        public void SetSongObject(AnysongObject newSong, int index)
        {
            this.songObject = newSong;
            if (_isPreviewing)
            {
                Load(newSong);
            }
        }

        public void SetSongPackIndex(int index)
        {
            _currentSongPackIndex = index;
        }
    }
}