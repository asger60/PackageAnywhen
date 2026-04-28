using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "AnywhenAudioPlayer", menuName = "Anywhen/Create AnywhenAudioPlayer asset", order = 2)]
public class AnywhenAudioGenrator : ScriptableObject, IAudioGenerator
{
    [SerializeField] AnysongObject song;

    private List<Action> _songChangedActions;

    private bool _sectionLockState;
    int _currentLockSectionIndex;
    private bool _isPlaying;
    private GeneratorInstance _generatorInstance;

    private void OnEnable()
    {
        _songChangedActions ??= new List<Action>();
        if (song != null)
        {
            song.RemoveListeners();
        }
    }

    private void OnDisable()
    {
        if (_sharedStepIndices.IsCreated) _sharedStepIndices.Dispose();
    }

    public void SetSectionLock(bool state, int lockedSectionIndex)
    {
        _currentLockSectionIndex = lockedSectionIndex;
        _sectionLockState = state;
    }

    public void SetSong(AnysongObject newSong)
    {
        song = newSong;
    }

    public void SetPlay(bool state)
    {
        _isPlaying = state;
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerPlaybackMsg(_isPlaying));
        }
    }

    public void Load(AnysongObject currentSong)
    {
        song = currentSong;
        foreach (var track in song.Tracks)
        {
            if (track.instrument is AnywhenSampleInstrument sampleInstrument)
            {
                if (!InstrumentDatabase.IsLoaded(sampleInstrument))
                {
                    InstrumentDatabase.LoadInstrumentNotes(sampleInstrument);
                }
            }
        }

        foreach (var action in _songChangedActions)
        {
            action?.Invoke();
        }

        Debug.Log("Loading song");
    }

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;
    public bool SectionLockState => _sectionLockState;

    private NativeArray<int> _sharedStepIndices;

    public int GetStepIndex(int trackIndex)
    {
        if (!_sharedStepIndices.IsCreated || trackIndex >= _sharedStepIndices.Length)
            return 0;
        return _sharedStepIndices[trackIndex];
    }


    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        int trackCount = song != null ? song.Tracks.Count : 0;
        if (_sharedStepIndices.IsCreated) _sharedStepIndices.Dispose();
        _sharedStepIndices = new NativeArray<int>(trackCount, Allocator.Persistent);
        _generatorInstance = Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, song, _sharedStepIndices);
        return _generatorInstance; // <-- still return it as before
    }


    public class TriggerPlaybackMsg
    {
        public readonly bool isPlaying;

        public TriggerPlaybackMsg(bool isPlaying)
        {
            this.isPlaying = isPlaying;
        }
    }

    public class TriggerNoteClipMsg
    {
        private readonly SimpleNoteEvent _noteEvent;
        private readonly double _scheduledPlayTime;

        public PlaybackEvent PlaybackEvent => new(_noteEvent, _scheduledPlayTime);

        public TriggerNoteClipMsg(SimpleNoteEvent noteEvent, double scheduledPlayTime)
        {
            _noteEvent = noteEvent;
            _scheduledPlayTime = scheduledPlayTime;
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    public struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<Track> _tracks;
        NativeArray<AnysongSection.Unmanaged> _anysongSections;
        private int _sampleRate;
        GeneratorInstance.Setup _setup;
        private GeneratorInstance _selfHandle;
        private int _lastSub16Count;
        private NativeArray<int> _stepIndices;

        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, AnysongObject initialSong,
            NativeArray<int> stepIndices)
        {
            var processor = new Processor(sampleRate, initialSong, stepIndices);
            var handle = context.AllocateGenerator(processor, new Control());
            processor._selfHandle = handle;
            return handle;
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;
        private bool _isPlaying; // <-- add

        Processor(int sampleRate, AnysongObject song, NativeArray<int> stepIndices)
        {
            _stepIndices = stepIndices; // <-- assign

            _sampleRate = sampleRate;
            _setup = new GeneratorInstance.Setup();
            _selfHandle = default;
            _lastSub16Count = -1;
            _anysongSections = default;
            _isPlaying = false;

            if (song != null)
            {
                _anysongSections = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Persistent);
                for (int i = 0; i < song.Sections.Count; i++)
                {
                    _anysongSections[i] = song.Sections[i].ToUnmanaged();
                }


                var sectionsRef = _anysongSections;
                Action<int, int, int> midiListener = (sectionIndex, trackIndex, patternIndex) =>
                {
                    if (!sectionsRef.IsCreated) return;

                    var section = sectionsRef[sectionIndex];
                    var track = section.tracks[trackIndex];
                    var pattern = track.Patterns[patternIndex];
                    var previousInternalIndex = track.Patterns[patternIndex].internalIndex;
                    pattern = song.Sections[sectionIndex].tracks[trackIndex].patterns[patternIndex].ToUnmanaged();
                    pattern.internalIndex = previousInternalIndex;
                    track.Patterns[patternIndex] = pattern;

                    section.tracks[trackIndex] = track;
                    sectionsRef[sectionIndex] = section;
                };
                song.OnSongMidiChanged += midiListener;

                _tracks = new NativeArray<Track>(song.Tracks.Count, Allocator.Persistent);

                for (int i = 0; i < _tracks.Length; i++)
                {
                    var trackSettings = song.Tracks[i];
                    if (trackSettings.instrument is AnywhenSampleInstrument)
                    {
                        var unmanagedSettings = trackSettings.ToUnmanaged();
                        _tracks[i] = new Track(_sampleRate, unmanagedSettings);

                        int capturedIndex = i;
                        var tracksRef = _tracks;
                        Action settingsListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnValuesChanged(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };
                        Action effectsListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnTrackRebuild(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };

                        song.OnSongSettingsChanged += settingsListener;
                        song.OnSongEffectsChanged += effectsListener;
                    }
                }
            }
            else
            {
                _tracks = new NativeArray<Track>(0, Allocator.Persistent);
            }
        }


        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            foreach (var element in pipe.GetAvailableData(context))
            {
                if (element.TryGetData(out PlaybackStateData data))
                {
                    _isPlaying = data.IsPlaying;
                    if (_isPlaying)
                    {
                        for (int trackIndex = 0; trackIndex < _anysongSections[0].tracks.Length; trackIndex++)
                        {
                            var section = _anysongSections[0];
                            var track = section.tracks[trackIndex];
                            track.Reset();
                            var pattern = track.Patterns[track.CurrentPatternIndex];
                            pattern.SyncToMetronome(AnywhenAudioMetronome.SharedSub16Count.Data);
                            track.Patterns[track.CurrentPatternIndex] = pattern;
                            section.tracks[trackIndex] = track;
                        }
                    }
                }
            }
        }


        public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe,
            ChannelBuffer buffer, GeneratorInstance.Arguments args)
        {
            double sampleRate = _setup.sampleRate;
            double invSampleRate = 1.0 / sampleRate;
            double dspTime = context.dspTime * invSampleRate;

            int currentSub16Count = AnywhenAudioMetronome.SharedSub16Count.Data;

            if (_isPlaying && currentSub16Count != _lastSub16Count)
            {
                for (int trackIndex = 0; trackIndex < _anysongSections[0].tracks.Length; trackIndex++)
                {
                    var section = _anysongSections[0];
                    var track = section.tracks[trackIndex];
                    if (currentSub16Count == 0)
                        track.AdvancePlayingPattern();

                    var pattern = track.Patterns[track.CurrentPatternIndex];


                    if (_stepIndices.IsCreated && trackIndex < _stepIndices.Length)
                    {
                        _stepIndices[trackIndex] = pattern.internalIndex;
                    }

                    var thisStep = pattern.GetCurrentStep();
                    if (thisStep.noteOn)
                    {
                        var playbackTrack = _tracks[trackIndex];
                        playbackTrack.HandlePlaybackEvent(new PlaybackEvent(thisStep, dspTime));
                        _tracks[trackIndex] = playbackTrack;
                    }

                    pattern.AdvancePlayingStep();
                    track.Patterns[track.CurrentPatternIndex] = pattern;
                    section.tracks[trackIndex] = track;
                    _anysongSections[0] = section;
                }

                _lastSub16Count = currentSub16Count;
            }

            if (sampleRate <= 0)
                return buffer.frameCount;

            // Clear buffer before mixing
            for (var frame = 0; frame < buffer.frameCount; frame++)
            {
                for (var channel = 0; channel < buffer.channelCount; channel++)
                    buffer[channel, frame] = 0;
            }

            for (int i = 0; i < _tracks.Length; i++)
            {
                var track = _tracks[i];
                for (var frame = 0; frame < buffer.frameCount; frame++)
                {
                    double currentFrameDspTime = dspTime + (frame * invSampleRate);
                    float trackAmp = track.Process(currentFrameDspTime);

                    for (var channel = 0; channel < buffer.channelCount; channel++)
                        buffer[channel, frame] += trackAmp;
                }

                _tracks[i] = track;
            }

            return buffer.frameCount;
        }

        struct Control : GeneratorInstance.IControl<Processor>
        {
            public void Configure(ControlContext context, ref Processor generator, in AudioFormat config,
                out GeneratorInstance.Setup setup, ref GeneratorInstance.Properties p)
            {
                generator._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator._setup;
            }

            public void Dispose(ControlContext context, ref Processor generator)
            {
                if (generator._tracks.IsCreated)
                {
                    for (int i = 0; i < generator._tracks.Length; i++)
                        generator._tracks[i].Dispose();
                    generator._tracks.Dispose();
                }
            }

            public void Update(ControlContext context, ProcessorInstance.Pipe pipe)
            {
            }

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe,
                ProcessorInstance.Message message)
            {
                if (message.Is<TriggerNoteClipMsg>())
                {
                    var payload = message.Get<TriggerNoteClipMsg>();
                    pipe.SendData(context, payload.PlaybackEvent);
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerPlaybackMsg>())
                {
                    var payload = message.Get<TriggerPlaybackMsg>();
                    pipe.SendData(context, new PlaybackStateData { IsPlaying = payload.isPlaying });
                    return ProcessorInstance.Response.Handled;
                }

                return ProcessorInstance.Response.Unhandled;
            }
        }


        public struct Track : IEquatable<Track>
        {
            int _sampleRate;
            private NativeArray<SampleVoice> _voices;
            private AnywhenSampleInstrument.Unmanaged _sampleInstrument;
            private float _trackVolume;
            private AudioProcessorEnvelope _trackEnvelope1, _trackEnvelope2;
            AudioProcessorLFO _trackLFO1, _trackLFO2;
            private float _trackLFO1Value;
            float _trackLFO2Value;
            private float _trackEnvelope1Value;
            private float _trackEnvelope2Value;

            private PlaybackEvent _nextEvent;
            private NativeArray<TrackAudioProcessor> _trackFilters;
            private float TrackLFO1Value => _trackLFO1Value;
            private float TrackLFO2Value => _trackLFO2Value;
            private float TrackEnvelope1Value => _trackEnvelope1Value;
            private float TrackEnvelope2Value => _trackEnvelope2Value;

            private bool _hasPendingCompleteUpdate;
            private bool _hasPendingParameterUpdate;
            private AnysongTrackSettings.Unmanaged _pendingSettings;

            public Track(int sampleRate, AnysongTrackSettings.Unmanaged settings) : this()
            {
                _sampleRate = sampleRate;
                _trackLFO1Value = 0;
                _trackLFO2Value = 0;

                CreateTrack(settings);
                UpdateSettings(settings);
                _nextEvent = new PlaybackEvent(new SimpleNoteEvent(), 0);
            }

            public void OnTrackRebuild(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingCompleteUpdate = true;
                _hasPendingParameterUpdate = true;
            }

            public void OnValuesChanged(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingParameterUpdate = true;
            }

            void CreateTrack(AnysongTrackSettings.Unmanaged settings)
            {
                if (_voices.IsCreated) _voices.Dispose();
                if (_trackFilters.IsCreated) _trackFilters.Dispose();
                _voices = new NativeArray<SampleVoice>(settings.voices, Allocator.Persistent);
                _trackEnvelope1 = new AudioProcessorEnvelope(_sampleRate);
                _trackEnvelope2 = new AudioProcessorEnvelope(_sampleRate);
                _trackLFO1 = new AudioProcessorLFO(_sampleRate);
                _trackLFO2 = new AudioProcessorLFO(_sampleRate);

                _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

                for (int i = 0; i < settings.trackFilters.Length; i++)
                {
                    _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
                }
            }

            void UpdateSettings(AnysongTrackSettings.Unmanaged settings)
            {
                _trackVolume = settings.volume;
                _sampleInstrument = settings.instrument;

                _trackEnvelope1.SetSettings(settings.TrackAudioEnvelope1.ToUnmanaged());
                _trackEnvelope2.SetSettings(settings.TrackAudioEnvelope2.ToUnmanaged());

                _trackEnvelope1Value = 1;
                _trackEnvelope2Value = 1;

                _trackLFO1.SetSettings(settings.TrackAudioLFO1.ToUnmanaged());
                _trackLFO2.SetSettings(settings.TrackAudioLFO2.ToUnmanaged());

                for (int i = 0; i < settings.trackFilters.Length; i++)
                {
                    var filter = _trackFilters[i];
                    filter.UpdateSettings(settings.trackFilters[i]);
                    _trackFilters[i] = filter;
                }
            }

            internal void HandlePlaybackEvent(PlaybackEvent playbackEvent)
            {
                _nextEvent = playbackEvent;

                int voiceToSteal = -1;
                double oldestStartTime = double.MaxValue;

                for (int i = 0; i < _voices.Length; i++)
                {
                    var voice = _voices[i];
                    if (voice.IsIdle)
                    {
                        voice.QueueNote(playbackEvent, ref _sampleInstrument);
                        _voices[i] = voice;
                        return;
                    }

                    if (voice.ScheduledStartTime < oldestStartTime)
                    {
                        oldestStartTime = voice.ScheduledStartTime;
                        voiceToSteal = i;
                    }

                    _voices[i] = voice;
                }

                if (voiceToSteal != -1)
                {
                    var voice = _voices[voiceToSteal];
                    voice.QueueNote(playbackEvent, ref _sampleInstrument);
                    _voices[voiceToSteal] = voice;
                }
            }

            internal float Process(double dspTime)
            {
                // Apply pending update on audio thread — safe to dispose here
                if (_hasPendingCompleteUpdate)
                {
                    CreateTrack(_pendingSettings);
                    _hasPendingCompleteUpdate = false;
                }

                if (_hasPendingParameterUpdate)
                {
                    UpdateSettings(_pendingSettings);
                    _hasPendingParameterUpdate = false;
                }


                if (!_voices.IsCreated)
                    return 0;

                _trackLFO1Value = _trackLFO1.Process((float)dspTime, this);
                _trackLFO2Value = _trackLFO2.Process((float)dspTime, this);
                _trackEnvelope1Value = _trackEnvelope1.Process((float)dspTime, this);
                _trackEnvelope2Value = _trackEnvelope2.Process((float)dspTime, this);

                if (dspTime >= _nextEvent.ScheduledPlayTime && dspTime < _nextEvent.ScheduledEndTime)
                {
                    _trackEnvelope1.SetGate(true);
                    _trackEnvelope2.SetGate(true);
                }
                else if (dspTime >= _nextEvent.ScheduledEndTime)
                {
                    _trackEnvelope1.SetGate(false);
                    _trackEnvelope2.SetGate(false);
                }

                float clipAmplitude = 0;
                for (int i = 0; i < _voices.Length; i++)
                {
                    var voice = _voices[i];
                    clipAmplitude += voice.Process(dspTime);
                    _voices[i] = voice;
                }

                if (_trackFilters.IsCreated)
                {
                    for (int i = 0; i < _trackFilters.Length; i++)
                    {
                        var filter = _trackFilters[i];
                        clipAmplitude = filter.Process(clipAmplitude, this);
                        _trackFilters[i] = filter;
                    }
                }

                return clipAmplitude * TrackEnvelope1Value * _trackVolume;
            }

            public float GetModSignal(NativeArray<SynthFilterBase.ModRouting> modRoutingSettings)
            {
                float s = 0;
                for (int i = 0; i < modRoutingSettings.Length; i++)
                {
                    var mod = modRoutingSettings[i];
                    float signal = mod.modSource switch
                    {
                        SynthFilterBase.ModRouting.ModSources.Envelope1 => TrackEnvelope1Value,
                        SynthFilterBase.ModRouting.ModSources.Envelope2 => TrackEnvelope2Value,
                        SynthFilterBase.ModRouting.ModSources.LFO1 => TrackLFO1Value,
                        SynthFilterBase.ModRouting.ModSources.LFO2 => TrackLFO2Value,
                        _ => 0f
                    };

                    s += signal * mod.modAmount;
                }

                return s;
            }

            public void Dispose()
            {
                if (_voices.IsCreated)
                    _voices.Dispose();

                if (_trackFilters.IsCreated)
                    _trackFilters.Dispose();
            }

            public bool Equals(Track other)
            {
                return _voices.Equals(other._voices) && _sampleInstrument.Equals(other._sampleInstrument) &&
                       _trackVolume.Equals(other._trackVolume) && _trackEnvelope1.Equals(other._trackEnvelope1) &&
                       _nextEvent.Equals(other._nextEvent);
            }

            public override bool Equals(object obj)
            {
                return obj is Track other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_voices, _sampleInstrument, _trackVolume, _trackEnvelope1, _nextEvent);
            }
        }

        private struct SampleVoice
        {
            private double _scheduledStartTime;
            private AnywhenNoteClip.Unmanaged _clipData;
            private int _sampleCount;
            private double _samplePosition;
            private bool _noteOn;
            private bool _noteQueued;
            private float _pitch;

            internal float Process(double dspTime)
            {
                float clipAmplitude = 0;
                if (_noteQueued && dspTime >= _scheduledStartTime)
                {
                    int channels = _clipData.channels;
                    int frameCount = channels > 0 ? _sampleCount / channels : 0;

                    if (_clipData.clipSamples.IsCreated && _samplePosition < frameCount)
                    {
                        int index = (int)_samplePosition;
                        float t = (float)(_samplePosition - index);

                        float frameAmplitude = 0;
                        bool canInterpolate = index < frameCount - 1;

                        for (int c = 0; c < channels; c++)
                        {
                            float s0 = _clipData.clipSamples[index * channels + c];
                            if (canInterpolate)
                            {
                                float s1 = _clipData.clipSamples[(index + 1) * channels + c];
                                frameAmplitude += s0 + t * (s1 - s0);
                            }
                            else
                            {
                                frameAmplitude += s0;
                            }
                        }

                        if (channels > 1)
                            frameAmplitude /= channels;

                        clipAmplitude = frameAmplitude;
                        _samplePosition += _pitch;
                    }
                    else
                    {
                        _noteQueued = false;
                    }
                }

                return clipAmplitude;
            }

            internal void QueueNote(PlaybackEvent playbackEvent, ref AnywhenSampleInstrument.Unmanaged sampleInstrument)
            {
                _noteQueued = true;
                _scheduledStartTime = playbackEvent.ScheduledPlayTime;
                var playbackSettings = sampleInstrument.GetNoteClipSettings(playbackEvent.SimpleNoteEvent.note);
                _clipData = playbackSettings.NoteClipUnmanaged;
                _pitch = playbackSettings.clipPitch;
                _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
                _samplePosition = 0;
            }

            public bool IsIdle => !_noteQueued;
            public double ScheduledStartTime => _scheduledStartTime;
        }
    }


    public struct PlaybackEvent : IEquatable<PlaybackEvent>
    {
        public SimpleNoteEvent SimpleNoteEvent;
        public readonly double ScheduledPlayTime;
        public readonly double ScheduledEndTime;

        public PlaybackEvent(AnysongPatternStep.UnManaged step, double scheduledPlayTime)
        {
            SimpleNoteEvent = new SimpleNoteEvent(step);
            ScheduledPlayTime = scheduledPlayTime + SimpleNoteEvent.drift;
            ScheduledEndTime = scheduledPlayTime + SimpleNoteEvent.duration + SimpleNoteEvent.drift;
        }

        public PlaybackEvent(SimpleNoteEvent simpleNoteEvent, double scheduledPlayTime)
        {
            SimpleNoteEvent = simpleNoteEvent;
            ScheduledPlayTime = scheduledPlayTime + simpleNoteEvent.drift;
            ScheduledEndTime = ScheduledPlayTime + SimpleNoteEvent.duration + simpleNoteEvent.drift;
        }

        public bool Equals(PlaybackEvent other)
        {
            return SimpleNoteEvent.Equals(other.SimpleNoteEvent) && ScheduledPlayTime.Equals(other.ScheduledPlayTime) &&
                   ScheduledEndTime.Equals(other.ScheduledEndTime);
        }

        public override bool Equals(object obj)
        {
            return obj is PlaybackEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SimpleNoteEvent, ScheduledPlayTime, ScheduledEndTime);
        }
    }

    public struct PlaybackStateData
    {
        public bool IsPlaying;
    }
}