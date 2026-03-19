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
        public class PlayerTrack
        {
            [FormerlySerializedAs("track")] public AnysongTrackSettings trackSettings;
            public AnywhenVoiceBase[] Voices;
            public float trackPitch;
            public List<SynthFilterBase> trackFilters;
            public SynthControlEnvelope trackEnvelope;
            public SynthControlLFO trackLFO;

            public PlayerTrack(AnysongTrackSettings trackSettings, AnywhenVoiceBase[] voices, List<SynthFilterBase> filters)
            {
                this.trackSettings = trackSettings;
                Voices = voices;
                trackPitch = trackSettings.TrackPitch;
                trackFilters = filters;
                trackLFO = new SynthControlLFO();
                trackLFO.UpdateSettings(trackSettings.trackLFO);
                trackEnvelope = new SynthControlEnvelope();
                trackEnvelope.UpdateSettings(trackSettings.trackEnvelope);
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
                    trackLFO.UpdateSettings(trackSettings.trackLFO);
                    trackEnvelope.UpdateSettings(trackSettings.trackEnvelope);

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

        private List<PlayerTrack> _tracksList = new();
        public List<PlayerTrack> TracksList => _tracksList;
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
            RebuildCaches();
            IsRunning = false;
            IsPlaying = false;
            Stop();
        }

        public void SetupTracks(List<AnysongTrackSettings> tracks)
        {
            _tracksList.Clear();

            for (var index = 0; index < tracks.Count; index++)
            {
                var anySongTrack = tracks[index];
                if (!anySongTrack.instrument) continue;
                var newPlayerTrack = new PlayerTrack(anySongTrack, null, null);

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
                        SynthSettingsObjectFilter.FilterTypes.ChorusFilter => new SynthFilterChorus(),
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

                newPlayerTrack.trackSettings = anySongTrack;
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
                RebuildCaches();
            }
        }

        private void OnTick16()
        {
            if (!IsRunning) return;
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }

        private Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongTrackSettings> _trackSettingsCache;
        private Dictionary<AnysongTrackSettings.AnyTrackTypes, PlayerTrack> _playerTrackCache;

        private void RebuildCaches()
        {
            _trackSettingsCache = new Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongTrackSettings>();
            _playerTrackCache = new Dictionary<AnysongTrackSettings.AnyTrackTypes, PlayerTrack>();
            
            foreach (var track in _tracksList)
            {
                if (track.trackSettings == null) continue;
                if (!_trackSettingsCache.ContainsKey(track.trackSettings.trackType))
                {
                    _trackSettingsCache.Add(track.trackSettings.trackType, track.trackSettings);
                }
                _playerTrackCache.TryAdd(track.trackSettings.trackType, track);
            }
        }

        AnysongTrackSettings GetTrackSettingsForTrackType(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            if (trackType == AnysongTrackSettings.AnyTrackTypes.None) return null;
            if (_trackSettingsCache == null) RebuildCaches();
            return _trackSettingsCache.GetValueOrDefault(trackType);
        }

        AnysongSectionTrack GetSectionTrackSettingsForTrackType(AnysongSection section,
            AnysongTrackSettings.AnyTrackTypes trackType)
        {
            return section.GetTrack(trackType);
        }

        private void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int i = 0; i < _currentSong.Tracks.Count; i++)
            {
                AnysongTrackSettings trackSettingsSettings = GetTrackSettingsForTrackType(_currentSong.Tracks[i].trackType);
                if (trackSettingsSettings == null)
                {
                    AnywhenRuntime.Log($"Track settings for track type {_currentSong.Tracks[i].trackType} is null");
                    continue;
                }

                if (trackSettingsSettings.IsMuted)
                {
                    continue;
                }

                var playerTrack = GetTrackForTrackType(trackSettingsSettings.trackType);
                if (playerTrack == null)
                {
                    AnywhenRuntime.Log($"Player track for track type {_currentSong.Tracks[i].trackType} is null");
                    continue;
                }

                //for (var sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)

                var section = _currentSong.Sections[CurrentSong.CurrentSectionIndex];
                var sectionTrack = GetSectionTrackSettingsForTrackType(section, trackSettingsSettings.trackType);
                if (sectionTrack == null)
                {
                    AnywhenRuntime.Log($"Section track for track type {_currentSong.Tracks[i].trackType} is null");
                    continue;
                }

                //var track = _currentSong.Tracks[trackSettings];
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


                if (!step.NoteOn) continue;

                float thisIntensity = Mathf.Clamp01(trackSettingsSettings.intensityMappingCurve.Evaluate(_currentIntensity));
                float thisRnd = Random.Range(0, 1f);

                if (thisRnd < step.chance && (1 - step.mixWeight) < thisIntensity && !_isMuted)
                {
                    TriggerNotePlayback(playerTrack, trackSettingsSettings, step);
                }
            }


            if (triggerStepIndex >= 0)
            {
                triggerStepIndex = -1;
            }
        }

        protected virtual void TriggerNotePlayback(PlayerTrack playerTrack, AnysongTrackSettings trackSettingsSettings,
            AnysongPatternStep step)
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

                playerTrack.playbackQueue.Add(playbackSettings);
            }

            var noteEvents = step.GetNoteEvents(0);

            foreach (var noteEvent in noteEvents)
            {
                HandleNoteEvent(noteEvent, trackSettingsSettings, _playerVolume);
            }
        }

        PlayerTrack GetTrackForTrackType(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            if (trackType == AnysongTrackSettings.AnyTrackTypes.None) return null;
            if (_playerTrackCache == null) RebuildCaches();
            return _playerTrackCache != null && _playerTrackCache.TryGetValue(trackType, out var track) ? track : null;
        }


        protected void HandleNoteEvent(NoteEvent noteEvent, AnysongTrackSettings trackSettings, float playerVolume)
        {
            for (var i = 0; i < noteEvent.notes.Length; i++)
            {
                var note = noteEvent.notes[i];
                var voice = GetVoice(trackSettings);
                if (voice == null)
                {
                    print("no voice for track: " + trackSettings);
                    continue;
                }

                var playTime = AnywhenMetronome.Instance.GetScheduledPlaytime() + noteEvent.drift + noteEvent.chordStrum[i];
                var volume = noteEvent.velocity * trackSettings.volume * playerVolume;
                var playbackSettings = new AnywhenVoiceBase.PlaybackSettings
                {
                    note = note,
                    playTime = playTime,
                    stopTime = playTime + noteEvent.duration + noteEvent.drift + trackSettings.trackEnvelope.release,
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
                RebuildCaches();
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

        protected virtual AnywhenVoiceBase GetVoice(AnysongTrackSettings trackSettings)
        {
            var playerTrack = GetTrackForTrackType(trackSettings.trackType);
            return playerTrack?.GetVoice();
        }

        bool IsDrums(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            return trackType is AnysongTrackSettings.AnyTrackTypes.Clap
                or AnysongTrackSettings.AnyTrackTypes.Snare
                or AnysongTrackSettings.AnyTrackTypes.Hihat
                or AnysongTrackSettings.AnyTrackTypes.Kick
                or AnysongTrackSettings.AnyTrackTypes.Tick
                or AnysongTrackSettings.AnyTrackTypes.Tom;
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

                    float amp = track.trackSettings.volume;
                    foreach (var volumeMod in track.trackSettings.volumeMods)
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
                track.trackSettings.trackEnvelope = newTrackSettings.Tracks[i].trackEnvelope;
                track.trackSettings.trackLFO = newTrackSettings.Tracks[i].trackLFO;
                track.trackPitch = newTrackSettings.Tracks[i].TrackPitch;
                track.trackSettings.volume = newTrackSettings.Tracks[i].volume;
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
        public void UpdateTrackInstrument(AnysongTrackSettings trackSettings)
        {
            bool didLoad = false;

            if (trackSettings.instrument is AnywhenSampleInstrument sampleInstrument)
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