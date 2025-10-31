using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Anywhen
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenPlayerBase : MonoBehaviour
    {
        private bool _isMuted;
        protected bool IsRunning;
        //private int _currentSectionIndex = 0;


        private float _playerVolume = 1;
        protected int _triggerStepIndex = -1;
        protected int CurrentBar;
        [SerializeField] private AnysongObject currentSong;
        public AnysongObject CurrentSong => currentSong;


        [SerializeField] protected AudioMixerGroup outputMixerGroup;

        public bool IsPlaying { get; private set; }

        bool _resetOnNextBar;

        [Serializable]
        public class PlayerVoices
        {
            public AnywhenInstrument instrument;
            public AnysongTrack track;

            public AnywhenVoiceBase[] Voices;

            public PlayerVoices(AnywhenInstrument instrument, AnysongTrack type, AnywhenVoiceBase[] voices)
            {
                this.instrument = instrument;
                track = type;
                Voices = voices;
            }

            public AnywhenVoiceBase GetVoice()
            {
                foreach (var voiceVoice in Voices)
                {
                    if (voiceVoice.HasScheduledPlay) continue;
                    if (voiceVoice.IsReady)
                    {
                        return voiceVoice;
                    }
                }

                float maxTime = float.MaxValue;
                AnywhenVoiceBase bestVoice = null;

                foreach (var voiceVoice in Voices)
                {
                    if (voiceVoice.HasScheduledPlay) continue;

                    var thisTime = voiceVoice.GetDurationToEnd();
                    if (thisTime < maxTime)
                    {
                        maxTime = thisTime;
                        bestVoice = voiceVoice;
                    }
                }

                return bestVoice ?? Voices[0];
            }
        }

        private List<PlayerVoices> _voicesList = new();
        public List<PlayerVoices> VoicesList => _voicesList;
        private AudioSource _audioSource;

        protected virtual void Start()
        {
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
            _audioSource.Play();
        }

        public void SetupVoices(List<AnysongTrack> tracks = null)
        {
            _voicesList.Clear();
            tracks ??= currentSong.Tracks;

            foreach (var songTrack in tracks)
            {
                if (!songTrack.instrument) continue;

                List<AnywhenVoiceBase> voices = new();

                for (int i = 0; i < songTrack.voices; i++)
                {
                    if (songTrack.instrument is AnywhenSampleInstrument)
                    {
                        var newVoice = new AnywhenSampleVoice();
                        newVoice.Init(AudioSettings.outputSampleRate, songTrack.instrument, songTrack);


                        voices.Add(newVoice);
                    }

                    if (songTrack.instrument is AnywhenSynthPreset preset)
                    {
                        var newSynthVoice = new AnywhenSynthVoice();
                        newSynthVoice.Init(AudioSettings.outputSampleRate, songTrack.instrument, songTrack);
                        voices.Add(newSynthVoice);
                    }
                }

                _voicesList.Add(new PlayerVoices(songTrack.instrument, songTrack, voices.ToArray()));
            }
        }

        private void OnTick16()
        {
            if (!IsRunning) return;
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }

        private void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
            {
                if (currentSong.Tracks[trackIndex].IsMuted) continue;

                for (var sectionIndex = 0; sectionIndex < currentSong.Sections.Count; sectionIndex++)
                {
                    var section = currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];


                    var track = currentSong.Tracks[trackIndex];
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


                    if (sectionIndex == CurrentSong.CurrentSectionIndex && (step.noteOn || step.noteOff))
                    {
                        float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(1));
                        float thisRnd = Random.Range(0, 1f);
                        if (thisRnd < step.chance && step.mixWeight < thisIntensity && !_isMuted)
                        {
                            TriggerNotePlayback(tickRate, trackIndex, step);
                        }
                    }
                }
            }


            if (_triggerStepIndex >= 0)
            {
                _triggerStepIndex = -1;
            }
        }

        protected virtual void TriggerNotePlayback(AnywhenMetronome.TickRate tickRate, int trackIndex, AnysongPatternStep step)
        {
            var songTrack = currentSong.Tracks[trackIndex];

            var noteEvents = step.GetNoteEvents(0);
            foreach (var noteEvent in noteEvents)
            {
                HandleNoteEvent(noteEvent, songTrack, _playerVolume);
            }
        }


        protected void HandleNoteEvent(NoteEvent noteEvent, AnysongTrack track, float playerVolume)
        {
            for (var i = 0; i < noteEvent.notes.Length; i++)
            {
                var note = noteEvent.notes[i];
                var voice = GetVoice(track);
                if (voice == null) continue;

                var playTime = AnywhenMetronome.Instance.GetScheduledPlaytime() +
                               (AnywhenMetronome.Instance.GetLength() * noteEvent.drift) +
                               noteEvent.chordStrum[i];

                var volume = noteEvent.velocity * track.volume * playerVolume;
                voice.NoteOn(note, playTime, playTime + noteEvent.duration, volume);
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

        protected virtual void ReleaseFromMetronome()
        {
            IsRunning = false;

            if (AnywhenRuntime.Metronome)
            {
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                AnywhenRuntime.Metronome.OnNextBar -= OnBar;
            }
        }

        protected virtual void AttachToMetronome()
        {
            if (IsRunning) return;
            IsRunning = true;
            if (AnywhenRuntime.Metronome)
            {
                AnywhenRuntime.Metronome.OnTick16 += OnTick16;
                AnywhenRuntime.Metronome.OnNextBar += OnBar;
            }
        }

        private bool _firstBar;

        protected virtual void OnBar()
        {
            if (!IsRunning) return;

            if (_resetOnNextBar)
            {
                CurrentBar = 0;
                _resetOnNextBar = false;
            }
            else
            {
                CurrentBar++;
            }

            if (AnywhenMetronome.Instance.CurrentBar % 4 == 0 && CurrentBar % 4 != 0)
            {
                CurrentBar = 0;
            }


            if (!_firstBar)
            {
                for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
                {
                    var track = currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                    track.AdvancePlayingPattern();
                }

                int progress = (int)Mathf.Repeat(CurrentBar, currentSong.Sections[CurrentSong.CurrentSectionIndex].sectionLength);
                if (progress == 0)
                {
                    NextSection();
                }
            }


            _firstBar = false;
        }


        public virtual void Play()
        {
            if (!currentSong)
            {
                return;
            }

            _firstBar = true;
            IsPlaying = true;

            SetupVoices(currentSong.Tracks);

            for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < currentSong.Sections.Count; sectionIndex++)
                {
                    var section = currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[0];
                    sectionTrack.Reset();
                }
            }


            CurrentBar = -1;
            ResetSection();
            AttachToMetronome();
        }

        public virtual void Stop()
        {
            IsPlaying = false;
            ReleaseFromMetronome();
        }

        protected void NextSection()
        {
            CurrentBar = 0;
            CurrentSong.AdvanceSection();
            ResetSection();
        }

        protected void ResetSection()
        {
            for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
            {
                var track = currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                track.Reset();
            }
        }

        protected virtual AnywhenVoiceBase GetVoice(AnysongTrack track)
        {
            foreach (var voice in _voicesList)
            {
                if (IsDrums(track.trackType) && voice.instrument != track.instrument) continue;
                if (track == voice.track)
                    return voice.GetVoice();
            }

            AnywhenRuntime.Log("no voice found for track " + track.trackType, AnywhenRuntime.DebugMessageType.Warning);
            return null;
        }

        bool IsDrums(AnysongTrack.AnyTrackTypes trackType)
        {
            return trackType is AnysongTrack.AnyTrackTypes.Clap
                or AnysongTrack.AnyTrackTypes.Snare
                or AnysongTrack.AnyTrackTypes.Hihat
                or AnysongTrack.AnyTrackTypes.Kick
                or AnysongTrack.AnyTrackTypes.Tick
                or AnysongTrack.AnyTrackTypes.Tom;
        }


        public void SetVolume(float value)
        {
            _playerVolume = value;
        }


        public void ResetOnNextBar()
        {
            _resetOnNextBar = true;

            for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < currentSong.Sections.Count; sectionIndex++)
                {
                    var section = currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    sectionTrack.ResetOnNextBar();
                    section.Reset();
                }
            }
        }

        public void Reset()
        {
            if (!currentSong) return;
            for (int trackIndex = 0; trackIndex < currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < currentSong.Sections.Count; sectionIndex++)
                {
                    var section = currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    sectionTrack.Reset();
                    section.Reset();
                }
            }
        }

        public void SetIsMuted(bool state)
        {
            _isMuted = state;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!IsRunning) return;

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0f;
            }


            // Mix in each voice group
            foreach (var voice in _voicesList)
            {
                foreach (var anywhenVoice in voice.Voices)
                {
                    var voiceDSP = anywhenVoice.UpdateDSP(data.Length, channels);
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] += voiceDSP[i];
                    }
                }
            }
        }

        public virtual void SetIntensity(float value)
        {
        }

        public virtual int[] EditorGetPlayingTrackPatternIndexes()
        {
            List<int> returnList = new List<int>();
            for (var i = 0; i < currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks.Count; i++)
            {
                var track = currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[i];

                returnList.Add(track.GetPlayingPatternIndex());
            }

            return returnList.ToArray();
        }

        public virtual void Load(AnysongObject anysong)
        {
            if (!anysong) return;
            currentSong = anysong;
            currentSong.Reset();
        }

        public void SetOututMixerGroup(AudioMixerGroup group)
        {
            outputMixerGroup = group;
            _audioSource.outputAudioMixerGroup = group;
        }

#if UNITY_EDITOR
        public void UpdateTrackInstrument(AnysongTrack track)
        {
            bool didLoad = false;
            if (track.instrument is AnywhenSampleInstrument sampleInstrument)
            {
                if (!InstrumentDatabase.IsLoaded(sampleInstrument))
                {
                    didLoad = true;
                    InstrumentDatabase.LoadInstrumentNotes(sampleInstrument);
                }
            }

            if (didLoad)
                SetupVoices(currentSong.Tracks);
        }

        public void LoadInstruments()
        {
            foreach (var track in currentSong.Tracks)
            {
                if (track.instrument is AnywhenSampleInstrument instrument)
                {
                    InstrumentDatabase.LoadInstrumentNotes(instrument);
                }
            }
        }
#endif
    }
}