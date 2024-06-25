using Anywhen.Composing;
using Anywhen.SettingsObjects;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen
{
    public class AnysongPlayer : MonoBehaviour
    {
        public AnysongObject songObject;
        private AnywhenInstrument[] _instruments;
        private AnysongObject _currentSong;
        private bool _isRunning;
        private bool _loaded = false;
        public bool IsSongLoaded => _loaded;
        public float intensity;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        int _currentSectionIndex = 0;


        private void Start()
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
            var thisSection = _currentSong.Sections[_currentSectionIndex];
            int progress = (int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, thisSection.sectionLength);
            if (progress == 0)
            {
                NextSection();
            }
        }

        private void OnTick16()
        {
            if (!_isRunning) return;

            if (_currentSong != songObject)
            {
                Release();
                Load(songObject);
            }

            int stepIndex = AnywhenRuntime.Metronome.Sub16;


            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                if (track.isMuted) continue;

                var songTrack = _currentSong.Tracks[trackIndex];
                AnywhenRuntime.Conductor.SetScaleProgression(_currentSong.Sections[0]
                    .GetProgressionStep(AnywhenMetronome.Instance.CurrentBar));

                var pattern = track.GetPattern(AnywhenMetronome.Instance.CurrentBar);
                var step = pattern.steps[stepIndex];
                float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(intensity));

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
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                _currentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }
            else
            {
                _currentSectionIndex++;
                _currentSectionIndex = (int)Mathf.Repeat(_currentSectionIndex, _currentSong.Sections.Count);
            }
        }

        public int GetStepForTrack(int trackIndex)
        {
            return AnywhenRuntime.Metronome.Sub16;
        }


        private void Release()
        {
            _loaded = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }


        public void SetMixIntensity(float value)
        {
        }

        public void ReleaseFromMetronome()
        {
            _isRunning = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }

        public void AttachToMetronome()
        {
            if (_isRunning) return;
            if (!_loaded) return;
            _isRunning = true;
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.Metronome.OnNextBar += OnBar;
        }

        public void Play()
        {
            AnysongPlayerBrain.TransitionTo(this, triggerTransitionsType);
        }

        public float GetTrackProgress()
        {
            int trackLength = _currentSong.Sections[_currentSectionIndex].patternSteps.Length;
            int progress = (int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, trackLength);
            return (float)progress / trackLength;
        }


        public void SetGlobalIntensity(float globalIntensity)
        {
            intensity = globalIntensity;
        }
    }
}