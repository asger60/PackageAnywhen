using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
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
            public List<SynthFilterBase> trackFilters;
            public SynthControlEnvelope trackEnvelope;
            public SynthControlLFO trackLFO;

            public PlayerTracks(AnywhenInstrument instrument, AnysongTrack track, AnywhenVoiceBase[] voices,
                List<SynthFilterBase> filters)
            {
                this.instrument = instrument;
                this.track = track;
                Voices = voices;
                trackPitch = track.TrackPitch;
                trackFilters = filters;
                trackLFO = new SynthControlLFO();
                trackLFO.UpdateSettings(track.trackLFO);
                trackEnvelope = new SynthControlEnvelope();
                trackEnvelope.UpdateSettings(track.trackEnvelope);
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


            public List<AnywhenVoiceBase.PlaybackSettings> playbackQueue = new();
            private AnywhenVoiceBase.PlaybackSettings _currentPlaybackSettings;

            public void HandleQueue()
            {
                while (playbackQueue.Count > 0 && AudioSettings.dspTime >= playbackQueue[0].playTime)
                {
                    trackLFO.UpdateSettings(track.trackLFO);
                    trackEnvelope.UpdateSettings(track.trackEnvelope);

                    _currentPlaybackSettings = playbackQueue[0];
                    playbackQueue.RemoveAt(0);
                    trackEnvelope.Reset();
                    trackEnvelope.NoteOn();
                    trackLFO.NoteOn();
                }

                if (_currentPlaybackSettings.note != -1 && AudioSettings.dspTime >= _currentPlaybackSettings.stopTime)
                {
                    _currentPlaybackSettings.note = -1;
                    trackEnvelope.NoteOff();
                    trackLFO.NoteOff();
                }
            }
        }

        private List<PlayerTracks> _tracksList = new();
        public List<PlayerTracks> TracksList => _tracksList;
        private AudioSource _audioSource;
        private float[] _trackBuffer;

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
                var newPlayerTrack = new PlayerTracks(anySongTrack.instrument, anySongTrack, null, null);

                List<SynthFilterBase> filters = new();

                foreach (var trackFilter in anySongTrack.TrackFilters)
                {
                    if (!trackFilter) continue;
                    trackFilter.modRouting ??= Array.Empty<SynthFilterBase.ModRouting>();
                    SynthFilterBase newFilter = trackFilter.filterType switch
                    {
                        SynthSettingsObjectFilter.FilterTypes.LowPassFilter => new SynthFilterLowPass(),
                        SynthSettingsObjectFilter.FilterTypes.BandPassFilter => new SynthFilterBandPass(),
                        SynthSettingsObjectFilter.FilterTypes.FormantFilter => new SynthFilterFormant(),
                        SynthSettingsObjectFilter.FilterTypes.LadderFilter => new SynthFilterLadder(),
                        SynthSettingsObjectFilter.FilterTypes.BitcrushFilter => new SynthFilterBitcrush(),
                        SynthSettingsObjectFilter.FilterTypes.SaturatorFilter => new SynthFilterSaturator(),
                        SynthSettingsObjectFilter.FilterTypes.DelayFilter => new SynthFilterDelay(),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    newFilter.SetSettings(trackFilter);
                    filters.Add(newFilter);
                    foreach (var modRouting in trackFilter.modRouting)
                    {
                        modRouting.Set(newPlayerTrack);
                        newFilter.AddModRouting(modRouting);
                    }
                }

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

                newPlayerTrack.track = anySongTrack;
                anySongTrack.volumeMods ??= Array.Empty<SynthFilterBase.ModRouting>();
                foreach (var volumeMod in anySongTrack.volumeMods)
                {
                    volumeMod.Set(newPlayerTrack);
                }

                anySongTrack.pitchMods ??= Array.Empty<SynthFilterBase.ModRouting>();
                foreach (var pitchMod in anySongTrack.pitchMods)
                {
                    pitchMod.Set(newPlayerTrack);
                }

                newPlayerTrack.Voices = voicesList.ToArray();
                newPlayerTrack.trackFilters = filters;
                _tracksList.Add(newPlayerTrack);
            }
        }

        private void OnTick16()
        {
            if (!IsRunning) return;
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }

        int GetTrackIndexOfType(AnysongTrack.AnyTrackTypes trackType)
        {
            if (trackType == AnysongTrack.AnyTrackTypes.None) return 0;
            for (var i = 0; i < _tracksList.Count; i++)
            {
                var track = _tracksList[i];
                if (track.track.trackType == trackType) return i;
            }

            return -1;
        }

        private void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int i = 0; i < _currentSong.Tracks.Count; i++)
            {
                int trackIndex = GetTrackIndexOfType(_currentSong.Tracks[i].trackType);
                if (trackIndex == -1)
                {
                    print("no track of type: " + _currentSong.Tracks[i].trackType);
                    continue;
                }

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

                        if (thisRnd < step.chance && (1 - step.mixWeight) < thisIntensity && !_isMuted)
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
            if (step.GetNoteEvents(0).Length > 0)
            {
                var nextStepEvent = step.GetNoteEvents(1)[0];
                var playTime = AnywhenMetronome.Instance.GetScheduledPlaytime() + nextStepEvent.drift;
                if (AudioSettings.dspTime > playTime)
                {
                    return;
                }

                var playbackSettings = new AnywhenVoiceBase.PlaybackSettings
                {
                    note = 0,
                    playTime = playTime,
                    stopTime = playTime + nextStepEvent.duration + nextStepEvent.drift,
                    volume = nextStepEvent.velocity
                };

                TracksList[trackIndex].playbackQueue.Add(playbackSettings);
            }

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
                    note = note,
                    playTime = playTime,
                    stopTime = playTime + noteEvent.duration + noteEvent.drift + track.trackEnvelope.release,
                    volume = volume
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


            for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
            {
                var track = _currentSong.Sections[CurrentSong.CurrentSectionIndex].tracks[trackIndex];
                track.AdvancePlayingPattern();
            }

            int progress = (int)Mathf.Repeat(CurrentBar, _currentSong.Sections[CurrentSong.CurrentSectionIndex].sectionLength);
            if (progress == 0)
            {
                NextSection();
            }
        }


        public virtual void Play(bool syncToGlobalTime = false)
        {
            if (!_currentSong)
            {
                AnywhenRuntime.Log("No song loaded.");
                return;
            }

            IsPlaying = true;
            _currentSong.Reset();
            CurrentBar = 0;

            if (syncToGlobalTime)
            {
                _currentSong.SyncToClock();
            }

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
                // We reuse a buffer to avoid garbage collection
                if (_trackBuffer == null || _trackBuffer.Length != data.Length)
                {
                    _trackBuffer = new float[data.Length];
                }
                else
                {
                    Array.Clear(_trackBuffer, 0, _trackBuffer.Length);
                }

                foreach (var anywhenVoice in track.Voices)
                {
                    var voiceDSP = anywhenVoice.UpdateDSP(data.Length, channels);
                    for (int i = 0; i < data.Length; i++)
                    {
                        _trackBuffer[i] += voiceDSP[i];
                    }
                }

                if (track.trackFilters.Count > 0)
                {
                    foreach (var filter in track.trackFilters)
                    {
                        if (filter == null) continue;


                        for (int i = 0; i < data.Length; i++)
                        {
                            _trackBuffer[i] = filter.Process(_trackBuffer[i]);
                        }
                    }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    track.HandleQueue();
                    track.trackEnvelope.DoUpdate();
                    track.trackLFO.DoUpdate();

                    float amp = track.track.volume;
                    foreach (var volumeMod in track.track.volumeMods)
                    {
                        amp = volumeMod.Process(amp);
                    }


                    data[i] += _trackBuffer[i] * amp;
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
                if (newTrackSettings.Tracks.Count <= i) continue;
                track.track.trackEnvelope = newTrackSettings.Tracks[i].trackEnvelope;
                track.track.trackLFO = newTrackSettings.Tracks[i].trackLFO;
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

        public virtual void SetMixAB(float mixValue)
        {
            AnywhenSnapshotBlender.ApplyBlend(_currentSong, mixValue, _tracksList);
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

        public virtual void LoadInstruments()
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