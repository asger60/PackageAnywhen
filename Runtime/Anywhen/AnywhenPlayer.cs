using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenPlayer")]
    public class AnywhenPlayer : AnywhenPlayerBase
    {
        public AnysongObject AnysongObject => songObject;
        private AnywhenInstrument[] _instruments;
        public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;
        public int currentSongPackIndex;
        private NoteEvent[] _lastTrackNote;

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


        [SerializeField] private AnysongObject customSong;
        [SerializeField] private bool playOnAwake;


        private void Start()
        {
            Load(songObject);
            SetupVoices(songObject.Tracks);
            if (playOnAwake)
                Play();
        }

        private void Load(AnysongObject anysong)
        {
            if (!anysong) return;
            CurrentSong = anysong;


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


        protected override void OnBar()
        {
            if (!IsRunning) return;
            CurrentBar++;
            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                CurrentSectionIndex = AnysongPlayerBrain.SectionLockIndex;
            }

            CurrentSectionIndex = Mathf.Min(CurrentSectionIndex, CurrentSong.Sections.Count - 1);

            var thisSection = CurrentSong.Sections[CurrentSectionIndex];
            int progress = (int)Mathf.Repeat(CurrentBar, thisSection.sectionLength);

            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                var track = CurrentSong.Sections[CurrentSectionIndex].tracks[trackIndex];
                track.AdvancePlayingPattern();
            }

            if (progress == 0)
            {
                NextSection();
            }


            var section = CurrentSong.Sections[CurrentSectionIndex];

            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar,
                CurrentSong.Sections[0]));
        }


        public float GetIntensity()
        {
            if (Application.isPlaying && followGlobalIntensity)
                return AnysongPlayerBrain.GlobalIntensity;

            return intensity;
        }

        void NextSection()
        {
            CurrentSong.Reset();
            CurrentBar = 0;
            if (!sectionsAutoAdvance)
                return;

            if (AnysongPlayerBrain.SectionLockIndex > -1)
            {
                SetSection(AnysongPlayerBrain.SectionLockIndex);
            }
            else
            {
                SetSection(CurrentSectionIndex + 1);

                for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
                {
                    var track = CurrentSong.Sections[CurrentSectionIndex].tracks[trackIndex];
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


        public override void Play()
        {
            if (!CurrentSong)
            {
                Load(AnysongObject);
            }

            
            
            if (!AnysongPlayerBrain.IsStarted)
            {
                AnywhenMetronome.Instance.SetTempo(currentPlayerTempo);
            }


            if (CurrentSong)
            {
                CurrentSong.Reset();
                CurrentSectionIndex = Random.Range(0, CurrentSong.Sections.Count - 1);
            }

            CurrentBar = 0;
            var section = CurrentSong.Sections[CurrentSectionIndex];

            SetupVoices(CurrentSong.Tracks);
            AttachToMetronome();
            AnysongPlayerBrain.RegisterPlay(this);
            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
        }

        public override void Stop()
        {
            AnysongPlayerBrain.RegisterStop(this);
            ReleaseFromMetronome();
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


        public void SetIntensity(float newIntensity)
        {
            intensity = newIntensity;
        }

        //void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        //{
        //    for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
        //    {
        //        for (var sectionIndex = 0; sectionIndex < CurrentSong.Sections.Count; sectionIndex++)
        //        {
        //            var section = CurrentSong.Sections[sectionIndex];
        //            var sectionTrack = section.tracks[trackIndex];
        //            if (sectionTrack.isMuted) continue;
//
//
        //            var track = CurrentSong.Tracks[trackIndex];
        //            var pattern = sectionTrack.GetPlayingPattern();
//
        //            var step = pattern.GetCurrentStep();
//
        //            if (stepIndex >= 0)
        //            {
        //                step = pattern.GetStep(stepIndex);
        //            }
        //            else if (_triggerStepIndex >= 0)
        //            {
        //                step = pattern.GetStep(_triggerStepIndex);
        //            }
//
        //            if (tickRate != AnywhenMetronome.TickRate.None)
        //                pattern.Advance();
//
//
        //            if (sectionIndex == CurrentSectionIndex && (step.noteOn || step.noteOff))
        //            {
        //                float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(GetIntensity()));
        //                float thisRnd = Random.Range(0, 1f);
        //                if (thisRnd < step.chance && step.mixWeight < thisIntensity)
        //                {
        //                    var songTrack = currentTracks[trackIndex];
//
        //                    var triggerStep = step.Clone();
        //                    triggerStep.rootNote += rootNoteMod;
//
        //                    songTrack.TriggerStep(step, pattern, tickRate, rootNoteMod);
        //                }
        //            }
        //        }
        //    }
//
        //    if (_triggerStepIndex >= 0)
        //    {
        //        _triggerStepIndex = -1;
        //    }
        //}

        public void TriggerStepIndex(int stepIndex, bool instant = false)
        {
            if (instant)
            {
                TriggerStepIndex(stepIndex);
            }
            else
            {
                _triggerStepIndex = stepIndex;
            }
        }

        public void SetStepIndex(int stepIndex)
        {
            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                foreach (var section in CurrentSong.Sections)
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
            CurrentSectionIndex = sectionIndex;
            CurrentSectionIndex = (int)Mathf.Repeat(CurrentSectionIndex, CurrentSong.Sections.Count);
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
            CurrentSectionIndex = sectionIndex;
        }

        public void EditorSetGlobelTempo(bool newValue)
        {
            followGlobalTempo = newValue;
        }

        public void EditorSetFollowGlobalIntensity(bool newValue)
        {
            followGlobalIntensity = newValue;
        }

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
            currentTracks = new AnysongTrack[CurrentSong.Tracks.Count];
            for (var i = 0; i < CurrentSong.Tracks.Count; i++)
            {
                var track = CurrentSong.Tracks[i];
                currentTracks[i] = new AnysongTrack();
                currentTracks[i] = track.Clone();
            }
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
            CurrentSong = anysongObject;
            Load(CurrentSong);
            if (CurrentSong != AnysongObject)
            {
                currentTracks = new AnysongTrack[CurrentSong.Tracks.Count];
                for (var i = 0; i < CurrentSong.Tracks.Count; i++)
                {
                    currentTracks[i] = CurrentSong.Tracks[i].Clone();
                }
            }


            if (CurrentSong == AnysongObject)
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


        public int[] EditorGetPlayingTrackPatternIndexes()
        {
            List<int> returnList = new List<int>();
            for (var i = 0; i < CurrentSong.Sections[CurrentSectionIndex].tracks.Count; i++)
            {
                var track = CurrentSong.Sections[CurrentSectionIndex].tracks[i];

                returnList.Add(track.GetPlayingPatternIndex());
            }

            return returnList.ToArray();
        }


        public void EditorSetTempo(int newTempo)
        {
            currentPlayerTempo = newTempo;
            AnywhenRuntime.Metronome.SetTempo(newTempo);
        }
    }
}