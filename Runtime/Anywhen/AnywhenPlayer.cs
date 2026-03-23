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


        protected override void Awake()
        {
            base.Awake();

            Load(songObject);
            SetupTracks(songObject.Tracks);
            if (playOnAwake)
                Play();
        }

        


        protected override void OnBar()
        {
            if (!IsRunning) return;
            CurrentBar++;
            if (CurrentSong.CurrentPlayMode == AnysongObject.SongPlayModes.Edit)
            {
                // todo, have editor set the section index
                ResetSection();
            }

            //CurrentSectionIndex = Mathf.Min(CurrentSectionIndex, CurrentSong.Sections.Count - 1);

            var thisSection = CurrentSong.Sections[CurrentSong.CurrentSectionIndex];
            int progress = (int)Mathf.Repeat(CurrentBar, thisSection.sectionLength);

            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                var track = CurrentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                track.AdvancePlayingPattern();
            }

            if (progress == 0)
            {
                NextSection();
            }


            var section = CurrentSong.Sections[CurrentSong.CurrentSectionIndex];
            AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
        }


        public float GetIntensity()
        {
            if (Application.isPlaying && followGlobalIntensity)
                return AnysongPlayerBrain.GlobalIntensity;

            return intensity;
        }


        private void OnDisable()
        {
            ReleaseFromMetronome();
        }

        private void OnDestroy()
        {
            ReleaseFromMetronome();
        }


        public override void Play(bool syncToGlobalTime = false)
        {
            if (!CurrentSong)
            {
                Load(AnysongObject);
            }


            if (!AnysongPlayerBrain.IsStarted)
            {
                AnywhenMetronome.Instance.SetTempo(currentPlayerTempo);
            }

            CurrentBar = 0;
            var section = CurrentSong.Sections[CurrentSong.CurrentSectionIndex];

            SetupTracks(CurrentSong.Tracks);
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


        public override void SetIntensity(float newIntensity)
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
                triggerStepIndex = stepIndex;
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


        public void ModifyIntensity(float newIntensity)
        {
            intensity += newIntensity;
        }


        public void EditorSetSection(int sectionIndex)
        {
            //CurrentSectionIndex = sectionIndex;
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
            Load(CurrentSong);
        }

        

        public void EditorSetTempo(int newTempo)
        {
            currentPlayerTempo = newTempo;
            AnywhenRuntime.Metronome.SetTempo(newTempo);
        }
    }
}