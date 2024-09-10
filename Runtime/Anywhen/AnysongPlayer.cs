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
        public float intensity;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        int _currentSectionIndex = 0;
        public int CurrentSectionIndex => _currentSectionIndex;

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
            _currentBar++;
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                _currentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }

            var thisSection = _currentSong.Sections[_currentSectionIndex];
            int progress = (int)Mathf.Repeat(_currentBar, thisSection.sectionLength);

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


            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                var pattern = track.GetPlayingPattern();
                var step = pattern.GetCurrentStep();
                pattern.Advance();


                if (track.isMuted) continue;


                var songTrack = _currentSong.Tracks[trackIndex];
                AnywhenRuntime.Conductor.SetScaleProgression(_currentSong.Sections[0].GetProgressionStep(AnywhenMetronome.Instance.CurrentBar));


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
            Debug.Log("next section");
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
            }
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
            print("attach to metro " + songObject.name);
        }

        public void Play()
        {
            print("play " + songObject.name);
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