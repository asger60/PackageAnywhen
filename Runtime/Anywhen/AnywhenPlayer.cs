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

        [SerializeField] private float intensity = 1;
        [SerializeField] private bool followGlobalTempo = false;
        [SerializeField] private bool followGlobalIntensity = false;


        [SerializeField] private bool playOnAwake;


        protected override void Start()
        {
            base.Start();
            Load(songObject);
            SetupVoices(songObject.Tracks);
            if (playOnAwake)
                Play();
        }

        private void Load(AnysongObject anysong)
        {
            if (!anysong) return;
            CurrentSong = anysong;
        }

#if UNITY_EDITOR
        public void LoadInstruments()
        {
            foreach (var track in CurrentSong.Tracks)
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



        public void EditorSetSongAndPackObject(AnysongObject newSong, int packIndex)
        {
            songObject = newSong;
            currentSongPackIndex = packIndex;
            if (AnywhenRuntime.IsPreviewing)
            {
                Load(newSong);
            }
        }


        public void EditorSetPreviewSong(AnysongObject anysongObject)
        {
            CurrentSong = anysongObject;
            Load(CurrentSong);
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