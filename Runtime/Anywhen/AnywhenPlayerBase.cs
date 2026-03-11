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
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenPlayerBase : MonoBehaviour
    {
        private bool _isMuted;
        protected bool IsRunning;

        private float _playerVolume = 1;
        protected int triggerStepIndex = -1;
        protected int CurrentBar;
        private AnysongObject _currentSong;
        public AnysongObject CurrentSong => _currentSong;

        float _currentIntensity = 1;
        [SerializeField] protected AudioMixerGroup outputMixerGroup;

        public bool IsPlaying { get; private set; }

        bool _resetOnNextBar;

        [Serializable]
        public class PlayerTracks
        {
            public AnywhenInstrument instrument;
            public AnysongTrack track;
            public AnywhenVoiceBase[] Voices;
            public float trackPitch;

            public PlayerTracks(AnywhenInstrument instrument, AnysongTrack track, AnywhenVoiceBase[] voices)
            {
                this.instrument = instrument;
                this.track = track;
                Voices = voices;
                trackPitch = track.TrackPitch;
            }

            public AnywhenVoiceBase GetVoice()
            {
                foreach (var voice in Voices)
                {
                    if (voice.HasScheduledPlay) continue;
                    if (voice.IsReady)
                    {
                        return voice;
                    }
                }

                float maxTime = 0;
                AnywhenVoiceBase bestVoice = null;

                foreach (var voice in Voices)
                {
                    var thisTime = voice.GetDurationToEnd();
                    if (thisTime > maxTime)
                    {
                        maxTime = thisTime;
                        bestVoice = voice;
                    }
                }

                return bestVoice ?? Voices[0];
            }
        }

        private List<PlayerTracks> _tracksList = new();
        public List<PlayerTracks> TracksList => _tracksList;
        private AudioSource _audioSource;

        protected virtual void Awake()
        {
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
            _audioSource.Play();
            _tracksList.Clear();
            IsRunning = false;
            IsPlaying = false;
            Stop();
        }

        public void SetupTracks(List<AnysongTrack> tracks)
        {
            _tracksList.Clear();

            for (var index = 0; index < tracks.Count; index++)
            {
                var anySongTrack = tracks[index];
                if (!anySongTrack.instrument) continue;

                List<AnywhenVoiceBase> voicesList = new();

                for (int i = 0; i < anySongTrack.voices; i++)
                {
                    AnywhenVoiceBase newTrack = null;
                    if (anySongTrack.instrument is AnywhenSynthPreset)
                    {
                        newTrack = new AnywhenSynthVoice(anySongTrack.instrument, anySongTrack);
                    }
                    else if (anySongTrack.instrument is AnywhenSampleInstrument)
                    {
                        newTrack = new AnywhenSampleVoice(anySongTrack.instrument, anySongTrack);
                    }

                    if (newTrack != null)
                    {
                        voicesList.Add(newTrack);
                    }
                }

                var instrument = anySongTrack.instrument;
                var track = anySongTrack;


                _tracksList.Add(new PlayerTracks(instrument, track, voicesList.ToArray()));
            }
        }

        private void OnTick16()
        {
            if (!IsRunning) return;
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }

        private void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                if (_currentSong.Tracks[trackIndex].IsMuted) continue;

                for (var sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
                {
                    var section = _currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];


                    var track = _currentSong.Tracks[trackIndex];
                    var pattern = sectionTrack.GetPlayingPattern();


                    var step = pattern.GetCurrentStep();

                    if (stepIndex >= 0)
                    {
                        step = pattern.GetStep(stepIndex);
                    }
                    else if (triggerStepIndex >= 0)
                    {
                        step = pattern.GetStep(triggerStepIndex);
                    }

                    if (tickRate != AnywhenMetronome.TickRate.None)
                        pattern.Advance();


                    if (sectionIndex == CurrentSong.CurrentSectionIndex && step.NoteOn)
                    {
                        float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(_currentIntensity));
                        float thisRnd = Random.Range(0, 1f);

                        if (thisRnd < step.chance && step.mixWeight < thisIntensity && !_isMuted)
                        {
                            TriggerNotePlayback(tickRate, trackIndex, step);
                        }
                    }
                }
            }


            if (triggerStepIndex >= 0)
            {
                triggerStepIndex = -1;
            }
        }

        protected virtual void TriggerNotePlayback(AnywhenMetronome.TickRate tickRate, int trackIndex, AnysongPatternStep step)
        {
            var songTrack = _currentSong.Tracks[trackIndex];
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
                if (voice == null)
                {
                    print("no voice for track: " + track);
                    continue;
                }

                var playTime = AnywhenMetronome.Instance.GetScheduledPlaytime() + noteEvent.drift + noteEvent.chordStrum[i];
                var volume = noteEvent.velocity * track.volume * playerVolume;
                var playbackSettings = new AnywhenVoiceBase.PlaybackSettings
                {
                    Note = note,
                    PlayTime = playTime,
                    StopTime = playTime + noteEvent.duration + noteEvent.drift,
                    Volume = volume
                };
                voice.NoteOn(playbackSettings);
            }
        }


        private void OnDisable()
        {
            ReleaseFromMetronome();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                _tracksList.Clear();
                _currentSong = null;
                IsPlaying = false;
                IsRunning = false;
                Stop();
            }

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
                for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
                {
                    var track = _currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                    track.AdvancePlayingPattern();
                }

                int progress = (int)Mathf.Repeat(CurrentBar,
                    _currentSong.Sections[CurrentSong.CurrentSectionIndex].sectionLength);
                if (progress == 0)
                {
                    NextSection();
                }
            }


            _firstBar = false;
        }


        public virtual void Play()
        {
            if (!_currentSong)
            {
                AnywhenRuntime.Log("No song loaded.");
                return;
            }

            _firstBar = true;
            IsPlaying = true;

            //SetupTracks(_currentSong.Tracks);

            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                foreach (var section in _currentSong.Sections)
                {
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
            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                track.Reset();
            }
        }

        protected virtual AnywhenVoiceBase GetVoice(AnysongTrack track)
        {
            foreach (var voice in _tracksList)
            {
                //if (IsDrums(track.trackType) && voice.instrument != track.instrument) continue;
                if (track == voice.track)
                    return voice.GetVoice();
            }

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

            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
                {
                    var section = _currentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    sectionTrack.ResetOnNextBar();
                    section.Reset();
                }
            }
        }

        public void Reset()
        {
            if (!_currentSong) return;
            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
                {
                    var section = _currentSong.Sections[sectionIndex];
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
            foreach (var track in _tracksList)
            {
                foreach (var anywhenVoice in track.Voices)
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
            _currentIntensity = value;
        }

        public virtual int[] EditorGetPlayingTrackPatternIndexes()
        {
            List<int> returnList = new List<int>();
            for (var i = 0; i < _currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks.Count; i++)
            {
                var track = _currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[i];

                returnList.Add(track.GetPlayingPatternIndex());
            }

            return returnList.ToArray();
        }

        public virtual void Load(AnysongObject anysong)
        {
            if (!anysong)
            {
                AnywhenRuntime.Log("Can't load, song is null");
                return;
            }

            SetupTracks(anysong.Tracks);


            _currentSong = anysong;
            _currentSong.Reset();
        }

        public virtual void SetTrackMidi(AnysongObject newTrackMidi)
        {
            _currentSong = newTrackMidi;
            _currentSong.Reset();
        }

        public virtual void SetTrackSettings(AnysongObject newTrackSettings)
        {
            for (var i = 0; i < _tracksList.Count; i++)
            {
                var track = _tracksList[i];
                track.track.trackEnvelope = newTrackSettings.Tracks[i].trackEnvelope;
                track.track.pitchLFOSettings = newTrackSettings.Tracks[i].pitchLFOSettings;
                track.trackPitch = newTrackSettings.Tracks[i].TrackPitch;
                track.track.volume = newTrackSettings.Tracks[i].volume;
            }
        }

        public void SetOutputMixerGroup(AudioMixerGroup group)
        {
            outputMixerGroup = group;
            if (!_audioSource) _audioSource = GetComponent<AudioSource>();
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
                SetupTracks(_currentSong.Tracks);
        }

        public void LoadInstruments()
        {
            foreach (var track in _currentSong.Tracks)
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