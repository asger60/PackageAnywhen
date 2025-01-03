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
        public AnysongObject AnysongObject => songObject;
        private AnywhenInstrument[] _instruments;
        private AnysongObject _currentSong;
        private bool _isRunning;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        int _currentSectionIndex = 0;
        public int CurrentSectionIndex => _currentSectionIndex;
        public int currentSongPackIndex;
        private NoteEvent[] _lastTrackNote;
        private int _currentBar;


        [SerializeField] private AnysongObject songObject;
        [SerializeField] private int currentPlayerTempo = -100;
        [SerializeField] private int rootNoteMod;
        //[SerializeField] private AnywhenTrigger trigger;
        [SerializeField] private AnysongTrack[] customTracks;
        [SerializeField] private AnysongTrack[] currentTracks;
        [SerializeField] private bool isCustomized;
        [SerializeField] private float intensity = 1;
        [SerializeField] private bool followGlobalTempo = false;
        [SerializeField] private bool followGlobalIntensity = false;
        [SerializeField] private bool sectionsAutoAdvance = true;


        [SerializeField] private AnysongObject customSong;
        private int _triggerStepIndex = -1;
        [SerializeField] private bool playOnAwake;

        private void Awake()
        {
           // trigger.OnTrigger += Play;
        }


        private void Start()
        {
            Load(songObject);
            if(playOnAwake)
                Play();
        }

        private void Load(AnysongObject anysong)
        {
            if (anysong == null) return;
            _currentSong = anysong;


            foreach (var track in currentTracks)
            {
                if (track.instrument is AnywhenSynthPreset preset)
                {
                    AnywhenRuntime.AnywhenSynthHandler.RegisterPreset(preset);
                }
            }
        }

#if UNITY_EDITOR
        public void LoadInstruments()
        {
            foreach (var track in currentTracks)
            {
                if (track.instrument is AnywhenSampleInstrument instrument)
                {
                    InstrumentDatabase.LoadInstrumentNotes(instrument);
                }
            }
        }
#endif


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
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }


        public float GetIntensity()
        {
            if (Application.isPlaying && followGlobalIntensity)
                return AnysongPlayerBrain.GlobalIntensity;

            return intensity;
        }

        void NextSection()
        {
            _currentSong.Reset();
            _currentBar = 0;
            if (!sectionsAutoAdvance)
                return;

            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                SetSection(AnysongPlayerBrain.SectionLockIndex);
            }
            else
            {
                SetSection(_currentSectionIndex + 1);

                for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
                {
                    var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                    track.Reset();
                }
            }
        }


        private void OnDisable()
        {
            ReleaseFromMetronome();
        }

        private void OnDestroy()
        {
            ReleaseFromMetronome();
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
            _isRunning = true;
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.Metronome.OnNextBar += OnBar;
            _currentSong.Reset();
        }

        public void Play()
        {
            if (_currentSong == null)
            {
                Load(AnysongObject);
            }

            if (!AnysongPlayerBrain.IsStarted)
            {
                AnywhenMetronome.Instance.SetTempo(currentPlayerTempo);
            }

            //if (Application.isPlaying && AnysongPlayerBrain.GetCurrentPlayer() == this)
            //{
            //    AnywhenRuntime.Log("retriggering player that is already playing");
            //    return;
            //}


            if (_currentSong)
            {
                _currentSong.Reset();
                _currentSectionIndex = Random.Range(0, _currentSong.Sections.Count - 1);
            }

            _currentBar = 0;
            var section = _currentSong.Sections[_currentSectionIndex];
            
            
            AttachToMetronome();
            AnysongPlayerBrain.RegisterPlay(this);

            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(_currentBar, _currentSong.Sections[0]));
            //AnysongPlayerBrain.TransitionTo(this, triggerTransitionsType);
        }

        public void Stop()
        {
            AnysongPlayerBrain.RegisterStop(this);
            ReleaseFromMetronome();
        }


        public void EditorSetSongAndPackObject(AnysongObject newSong, int packIndex)
        {
            songObject = newSong;
            currentSongPackIndex = packIndex;
            if (AnywhenRuntime.IsPreviewing)
            {
                Load(newSong);
            }

            currentTracks = new AnysongTrack[newSong.Tracks.Count];
            for (var i = 0; i < newSong.Tracks.Count; i++)
            {
                var track = newSong.Tracks[i];
                currentTracks[i] = new AnysongTrack();
                currentTracks[i] = track;
            }

            isCustomized = false;
        }


        public void EditorSetPreviewSong(AnysongObject anysongObject)
        {
            _currentSong = anysongObject;
            Load(_currentSong);
            if (_currentSong != AnysongObject)
            {
                AnywhenRuntime.Log("preview other song");
                currentTracks = new AnysongTrack[_currentSong.Tracks.Count];
                for (var i = 0; i < _currentSong.Tracks.Count; i++)
                {
                    currentTracks[i] = _currentSong.Tracks[i].Clone();
                }
            }


            if (_currentSong == AnysongObject)
            {
                if (isCustomized)
                {
                    currentTracks = new AnysongTrack[customTracks.Length];
                    for (var i = 0; i < customTracks.Length; i++)
                    {
                        currentTracks[i] = customTracks[i].Clone();
                    }
                }
                else
                {
                    currentTracks = new AnysongTrack[AnysongObject.Tracks.Count];
                    for (var i = 0; i < AnysongObject.Tracks.Count; i++)
                    {
                        currentTracks[i] = AnysongObject.Tracks[i];
                    }
                }
            }
        }

        //public void EditorCreateTrigger()
        //{
        //    trigger = gameObject.AddComponent<AnywhenTrigger>();
        //}
//
        //public void EditorLocateTrigger()
        //{
        //    trigger = GetComponentInChildren<AnywhenTrigger>();
        //}


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


        public void EditorSetTempo(int newTempo)
        {
            currentPlayerTempo = newTempo;
            AnywhenRuntime.Metronome.SetTempo(newTempo);
        }

        public int GetTempo()
        {
            if (currentPlayerTempo == -1)
                return AnysongObject.tempo;

            return currentPlayerTempo;
        }

        public int GetRootNote()
        {
            return rootNoteMod;
        }

        public bool GetUseGlobalIntensity() => followGlobalIntensity;
        public bool GetUseGlobalTempo() => followGlobalTempo;


        public void EditorSetRootNote(int newValue)
        {
            rootNoteMod = newValue;
        }

        public void EditorRandomizeSounds()
        {
            customTracks = new AnysongTrack[currentTracks.Length];
            for (var i = 0; i < currentTracks.Length; i++)
            {
                if (currentTracks[i].trackType != AnysongTrack.AnyTrackTypes.None)
                {
                    var newInstrument =
                        AnywhenRuntime.InstrumentDatabase.GetInstrumentOfType(currentTracks[i].trackType);
                    if (newInstrument)
                    {
                        currentTracks[i].instrument = newInstrument;
                    }
                }

                customTracks[i] = currentTracks[i].Clone();
            }

            isCustomized = true;
        }

        public void EditorRestoreSounds()
        {
            currentTracks = new AnysongTrack[_currentSong.Tracks.Count];
            for (var i = 0; i < _currentSong.Tracks.Count; i++)
            {
                var track = _currentSong.Tracks[i];
                currentTracks[i] = new AnysongTrack();
                currentTracks[i] = track.Clone();
            }
        }

        public void SetIntensity(float newIntensity)
        {
            intensity = newIntensity;
        }

        void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
                {
                    var section = _currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    if (sectionTrack.isMuted) continue;


                    var track = _currentSong.Tracks[trackIndex];
                    var pattern = sectionTrack.GetPlayingPattern();

                    var step = pattern.GetCurrentStep();

                    if (stepIndex >= 0)
                    {
                        step = pattern.GetStep(stepIndex);
                    }
                    else if (_triggerStepIndex >= 0)
                    {
                        step = pattern.GetStep(_triggerStepIndex);
                    }

                    if (tickRate != AnywhenMetronome.TickRate.None)
                        pattern.Advance();


                    if (sectionIndex == _currentSectionIndex && (step.noteOn || step.noteOff))
                    {
                        float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(GetIntensity()));
                        float thisRnd = Random.Range(0, 1f);
                        if (thisRnd < step.chance && step.mixWeight < thisIntensity)
                        {
                            var songTrack = currentTracks[trackIndex];

                            var triggerStep = step.Clone();
                            triggerStep.rootNote += rootNoteMod;

                            songTrack.TriggerStep(step, pattern, tickRate, rootNoteMod);
                        }
                    }
                }
            }

            if (_triggerStepIndex >= 0)
            {
                _triggerStepIndex = -1;
            }
        }

        public void TriggerStepIndex(int stepIndex, bool instant = false)
        {
            if (instant)
            {
                TriggerStep(stepIndex, AnywhenMetronome.TickRate.None);
            }
            else
            {
                _triggerStepIndex = stepIndex;
            }
        }

        public void SetStepIndex(int stepIndex)
        {
            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                foreach (var section in _currentSong.Sections)
                {
                    var sectionTrack = section.tracks[trackIndex];
                    foreach (var pattern in sectionTrack.patterns)
                    {
                        pattern.SetStepIndex(stepIndex);
                    }
                }
            }
        }

        public void SetSection(int sectionIndex)
        {
            _currentSectionIndex = sectionIndex;
            _currentSectionIndex = (int)Mathf.Repeat(_currentSectionIndex, _currentSong.Sections.Count);
        }

        public void SetSectionsAutoAdvance(bool state)
        {
            sectionsAutoAdvance = state;
        }

        public void ModifyIntensity(float newIntensity)
        {
            intensity += newIntensity;
        }


        public void EditorSetSection(int sectionIndex)
        {
            _currentSectionIndex = sectionIndex;
        }

        public void EditorSetGlobelTempo(bool newValue)
        {
            followGlobalTempo = newValue;
        }

        public void EditorSetFollowGlobalIntensity(bool newValue)
        {
            followGlobalIntensity = newValue;
        }
    }
}