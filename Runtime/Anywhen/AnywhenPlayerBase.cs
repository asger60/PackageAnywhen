using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;
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

        public bool IsPlaying { get; private set; }

        private Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongTrackSettings> _trackSettingsCache;
        private Dictionary<AnysongTrackSettings.AnyTrackTypes, PlayerTrack> _playerTrackCache;

        [Serializable]
        public class PlayerTrack
        {
            public AnysongTrackSettings trackSettings;
            public AnywhenVoiceBase[] Voices;
            public float trackPitch;
            public List<SynthFilterBase> trackFilters;
            public AudioProcessorEnvelope trackEnvelope;
            public AudioProcessorLFO trackLFO;

            public PlayerTrack(AnysongTrackSettings trackSettings, AnywhenVoiceBase[] voices, List<SynthFilterBase> filters)
            {
                this.trackSettings = trackSettings;
                Voices = voices;
                trackPitch = trackSettings.TrackPitch;
                trackFilters = filters;
                trackLFO = new AudioProcessorLFO();
               // trackLFO.UpdateSettings(trackSettings.trackAudioLFO);
                trackEnvelope = new AudioProcessorEnvelope();
                //trackEnvelope.UpdateSettings(trackSettings.trackAudioEnvelope);
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
                    //trackLFO.UpdateSettings(trackSettings.trackAudioLFO);
                    //trackEnvelope.UpdateSettings(trackSettings.trackAudioEnvelope);

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
                    //trackLFO.NoteOff();
                }
            }
        }

        private List<PlayerTrack> _tracksList = new();
        public List<PlayerTrack> TracksList => _tracksList;
        public int CurrentSectionIndex => _currentSectionIndex;
        public bool SectionLockState => _sectionLockState;

        private AudioSource _audioSource;
        private float[] _trackBuffer;
        private int _currentSectionIndex;

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
/*
                if (anySongTrack.TrackFilters != null)
                {
                    foreach (var trackFilter in anySongTrack.TrackFilters)
                    {
                        if (!trackFilter) continue;
                        //var trackFilterCopy = Instantiate(trackFilter);
                        trackFilter.modRouting ??= Array.Empty<SynthFilterBase.ModRouting>();
                        SynthFilterBase newFilter = trackFilter.filterType switch
                        {
                          //  SynthSettingsObjectFilter.FilterTypes.LowPassFilter => new AudioProcessorLowPass(),
                            //AudioProcessorSettingsObject.FilterTypes.BandPassFilter => new SynthFilterBandPass(),
                            //AudioProcessorSettingsObject.FilterTypes.FormantFilter => new SynthFilterFormant(),
                            //AudioProcessorSettingsObject.FilterTypes.LadderFilter => new SynthFilterLadder(),
                            //AudioProcessorSettingsObject.FilterTypes.BitcrushFilter => new SynthFilterBitcrush(),
                            //AudioProcessorSettingsObject.FilterTypes.SaturatorFilter => new AudioProcessorSaturator(),
                            //AudioProcessorSettingsObject.FilterTypes.DelayFilter => new SynthFilterDelay(),
                            //AudioProcessorSettingsObject.FilterTypes.ChorusFilter => new SynthFilterChorus(),
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
                }
*/
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
                //anySongTrack.volumeMods ??= new NativeArray<SynthFilterBase.ModRouting>(0, Allocator.Persistent);

                foreach (var volumeMod in anySongTrack.volumeMods)
                {
                    volumeMod.Set(newPlayerTrack);
                }

                //anySongTrack.pitchMods ??= new NativeArray<SynthFilterBase.ModRouting>(0, Allocator.Persistent);
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


        protected void RebuildCaches()
        {
            _trackSettingsCache = new Dictionary<AnysongTrackSettings.AnyTrackTypes, AnysongTrackSettings>();
            _playerTrackCache = new Dictionary<AnysongTrackSettings.AnyTrackTypes, PlayerTrack>();

            foreach (var track in _tracksList)
            {
                //if (track.trackSettings == null) continue;
                if (!_trackSettingsCache.ContainsKey(track.trackSettings.trackType))
                {
                    _trackSettingsCache.Add(track.trackSettings.trackType, track.trackSettings);
                }

                _playerTrackCache.TryAdd(track.trackSettings.trackType, track);
            }
        }

        AnysongTrackSettings GetTrackSettingsForTrackType(AnysongTrackSettings.AnyTrackTypes trackType)
        {
            if (trackType == AnysongTrackSettings.AnyTrackTypes.None) return new AnysongTrackSettings();
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
                //if (trackSettingsSettings == null)
                //{
                //    AnywhenRuntime.Log($"Track settings for track type {_currentSong.Tracks[i].trackType} is null");
                //    continue;
                //}

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

                var section = _currentSong.Sections[_currentSectionIndex];
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

                //float thisIntensity = Mathf.Clamp01(trackSettingsSettings.intensityMappingCurve.Evaluate(_currentIntensity));
                float thisIntensity = 1;
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

        protected virtual void TriggerNotePlayback(PlayerTrack playerTrack, AnysongTrackSettings trackSettingsSettings, AnysongPatternStep step)
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
                    stopTime = playTime + noteEvent.duration + noteEvent.drift + trackSettings.trackAudioEnvelope.release,
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

            CurrentBar++;

            int sectionLength = _currentSong.Sections[_currentSectionIndex].sectionLength;

            if (CurrentBar == sectionLength)
            {
                CurrentBar = 0;
                NextSection();
            }
            else if (!_firstBar)
            {
                for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
                {
                    var track = _currentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                    track.AdvancePlayingPattern();
                }
            }

            _firstBar = false;
        }

        protected void NextSection()
        {
            if (_sectionLockState)
            {
                _currentSectionIndex = _currentLockSectionIndex;
            }
            else
            {
                _currentSectionIndex++;
                if (_currentSectionIndex >= _currentSong.Sections.Count)
                    _currentSectionIndex = 0;
            }

            GetPlayingSection().Reset();
        }

        public virtual void Load(AnysongObject anysong)
        {
            if (!anysong)
            {
                AnywhenRuntime.Log("Can't load, song is null");
                return;
            }

            _currentSong = anysong;
            SetupTracks(_currentSong.Tracks);
        }

        private bool _firstBar;

        public virtual void Play(bool syncToGlobalTime = false)
        {
            if (!_currentSong)
            {
                AnywhenRuntime.Log("No song loaded.");
                return;
            }

            IsPlaying = true;
            _firstBar = true;
            CurrentBar = -1;
            _currentSectionIndex = 0;
            if (_sectionLockState)
                _currentSectionIndex = _currentLockSectionIndex;

            _currentSong.Reset();
            if (Application.isPlaying) _currentSong.UnMuteAll();

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


        protected virtual AnywhenVoiceBase GetVoice(AnysongTrackSettings trackSettings)
        {
            var playerTrack = GetTrackForTrackType(trackSettings.trackType);
            return playerTrack?.GetVoice();
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


        private void OnAudioFilterRead(float[] data, int channels)
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

        public void SetVolume(float value)
        {
            _playerVolume = value;
        }


        public void SetIsMuted(bool state)
        {
            _isMuted = state;
        }

        public virtual void SetIntensity(float value)
        {
            _currentIntensity = value;
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
                track.trackSettings.trackAudioEnvelope = newTrackSettings.Tracks[i].trackAudioEnvelope;
                track.trackSettings.trackAudioLFO = newTrackSettings.Tracks[i].trackAudioLFO;
                track.trackPitch = newTrackSettings.Tracks[i].TrackPitch;
                track.trackSettings.volume = newTrackSettings.Tracks[i].volume;
            }
        }


        public virtual void SetMixAB(float mixValue)
        {
            AnywhenSnapshotBlender.ApplyBlend(_currentSong, mixValue, _tracksList);
        }

        private bool _sectionLockState;
        int _currentLockSectionIndex;

        public void SetSectionLock(bool state, int lockedSectionIndex)
        {
            _currentLockSectionIndex = lockedSectionIndex;
            _sectionLockState = state;
        }

        public int GetPlayingSectionIndex()
        {
            return _currentSectionIndex;
        }

        public AnysongSection GetPlayingSection()
        {
            return _currentSong.Sections[_currentSectionIndex];
        }

        public int GetPlayingPatternIndexForTrackIndex(int trackIndex)
        {
            return GetPlayingSection().tracks[trackIndex].GetPlayingPatternIndex();
        }

        public AnysongPattern GetPlayingPatternForTrackIndex(int trackIndex)
        {
            return GetPlayingSection().tracks[trackIndex].patterns[GetPlayingSectionIndex()];
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