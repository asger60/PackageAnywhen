using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenPlayer")]
    public class AnywhenPlayer : MonoBehaviour
    {
        [SerializeField] private AnysongObject songObject;
        public AnysongObject AnysongObject => songObject;
        private AnywhenInstrument[] _instruments;
        private AnysongObject _currentSong, _previewSong;
        private bool _isRunning;
        private bool _loaded = false;
        public bool IsSongLoaded => _loaded;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        int _currentSectionIndex = 0;
        public int CurrentSectionIndex => _currentSectionIndex;
        [SerializeField] private AnywhenTrigger trigger;
        public int currentSongPackIndex;
        private float _previewIntensity = 1;
        private NoteEvent[] _lastTrackNote;
        private int _currentBar;

        private void Awake()
        {
            trigger.OnTrigger += Play;
            _previewSong = null;
        }

        public void Load()
        {
            Load(_previewSong ? _previewSong : songObject);
        }

        private void Start()
        {
            Load(songObject);
        }

        private void Load(AnysongObject anysong)
        {
            if (anysong == null) return;
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


        private void OnBar()
        {
            if (!_isRunning) return;
            _currentBar++;
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                _currentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }

            _currentSectionIndex = Mathf.Min(_currentSectionIndex, _currentSong.Sections.Count - 1);

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

            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(_currentBar,
                _currentSong.Sections[0]));
        }


        private void OnTick16()
        {
            if (!_isRunning) return;


            _currentSectionIndex = Mathf.Min(_currentSectionIndex, _currentSong.Sections.Count - 1);

            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var sectionTrack = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                var track = _currentSong.Tracks[trackIndex];
                var pattern = sectionTrack.GetPlayingPattern();
                var step = pattern.GetCurrentStep();
                pattern.Advance();


                if (sectionTrack.isMuted) continue;


                if (step.noteOn || step.noteOff)
                {
                    float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(GetIntensity()));
                    float thisRnd = Random.Range(0, 1f);
                    if (thisRnd < step.chance && step.mixWeight < thisIntensity)
                    {
                        var songTrack = _currentSong.Tracks[trackIndex];
                        songTrack.TriggerStep(step, pattern);
                    }
                }
            }
        }

        float GetIntensity()
        {
            if (Application.isPlaying)
                return AnysongPlayerBrain.GlobalIntensity;

            return _previewIntensity;
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


        protected internal void ReleaseFromMetronome()
        {
            _isRunning = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            AnywhenRuntime.Metronome.OnNextBar -= OnBar;
        }

        protected internal void AttachToMetronome()
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
            if (AnysongObject == null) return;
            if (!_currentSong) Load();

            if (!AnysongPlayerBrain.IsStarted)
                AnywhenMetronome.Instance.SetTempo(_currentSong.tempo);
            _currentSong.Reset();
            _currentSectionIndex = 0;
            _currentBar = 0;
            var section = _currentSong.Sections[_currentSectionIndex];
            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(_currentBar,
                _currentSong.Sections[0]));

            AnysongPlayerBrain.TransitionTo(this, triggerTransitionsType);
        }

        public float GetTrackProgress()
        {
            int trackLength = _currentSong.Sections[_currentSectionIndex].patternSteps.Length;
            int progress = (int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, trackLength);
            return (float)progress / trackLength;
        }

        

        public void EditorSetSongAndPackObject(AnysongObject newSong, int packIndex)
        {
            this.songObject = newSong;
            currentSongPackIndex = packIndex;
            if (AnywhenRuntime.IsPreviewing)
            {
                Load(newSong);
            }
        }


        public void EditorSetPreviewSong(AnysongObject anysongObject)
        {
            _previewSong = anysongObject;
        }

        public void EditorCreateTrigger()
        {
            trigger = gameObject.AddComponent<AnywhenTrigger>();
        }

        public void EditorLocateTrigger()
        {
            trigger = GetComponentInChildren<AnywhenTrigger>();
        }

        public void EditorSetTestIntensity(float value)
        {
            _previewIntensity = value;
        }

        public int[] EditorGetPlayingTrackPatternIndexes()
        {
            List<int> returnList = new List<int>();
            for (var i = 0; i < _currentSong.Sections[_currentSectionIndex].tracks.Count; i++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[i];

                returnList.Add(track.GetPlayingPatternIndex());
            }

            return returnList.ToArray();
        }
    }
}