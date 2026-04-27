using System;
using System.Collections.Generic;
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

    private readonly List<Action> _songChangedActions = new();

    private bool _sectionLockState;
    int _currentLockSectionIndex;

    public void SetSectionLock(bool state, int lockedSectionIndex)
    {
        _currentLockSectionIndex = lockedSectionIndex;
        _sectionLockState = state;
    }

    public void SetSong(AnysongObject newSong)
    {
        song = newSong;
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

    private void OnDisable()
    {
        foreach (var action in _songChangedActions)
            song.OnSongChanged -= action;
        _songChangedActions.Clear();
    }

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;
    public bool SectionLockState => _sectionLockState;

    

    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        return Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, song, _songChangedActions);
    }


    public class TriggerNoteClipMsg
    {
        private readonly SimpleNoteEvent _noteEvent;
        private readonly double _scheduledPlayTime;
        private readonly int _trackIndex;

        public PlaybackEvent PlaybackEvent => new(_noteEvent, _scheduledPlayTime, _trackIndex);

        public TriggerNoteClipMsg(SimpleNoteEvent noteEvent, double scheduledPlayTime, int trackIndex)
        {
            _noteEvent = noteEvent;
            _scheduledPlayTime = scheduledPlayTime;
            _trackIndex = trackIndex;
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    public struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<Track> _tracks;
        private int _sampleRate;
        GeneratorInstance.Setup _setup;
        private GeneratorInstance _selfHandle;

        private int _lastSub16Count;


        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, AnysongObject initialSong,
            List<Action> actionRegistry)
        {
            var processor = new Processor(sampleRate, initialSong, actionRegistry);
            var handle = context.AllocateGenerator(processor, new Control());
            processor._selfHandle = handle;
            return handle;
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;


        Processor(int sampleRate, AnysongObject song = null, List<Action> actionRegistry = null)
        {
            seed = 12345;
            _sampleRate = sampleRate;
            _setup = new GeneratorInstance.Setup();
            _selfHandle = default;
            _lastSub16Count = -1;
            _disposed = default;
            
            if (song != null)
            {
                _tracks = new NativeArray<Track>(song.Tracks.Count, Allocator.Persistent);
                
                _disposed = new NativeArray<byte>(1, Allocator.Persistent);
                _disposed[0] = 0;

                for (int i = 0; i < _tracks.Length; i++)
                {
                    var trackSettings = song.Tracks[i];
                    if (trackSettings.instrument is AnywhenSampleInstrument sampleInstrument)
                    {
                        var unmanagedSettings = trackSettings.ToUnmanaged();
                        _tracks[i] = new Track(_sampleRate, unmanagedSettings);
                        int capturedIndex = i;
                        var tracksSnapshot = _tracks;

                        var disposedRef = _disposed;

                        Action onSongChanged = () =>
                        {
                            if (!disposedRef.IsCreated || disposedRef[0] == 1) return;
                            try
                            {
                                var t = tracksSnapshot[capturedIndex];
                                t.OnValuesChanged(trackSettings.ToUnmanaged());
                                tracksSnapshot[capturedIndex] = t;
                            }
                            catch (ObjectDisposedException)
                            {
                                disposedRef[0] = 1;
                            }
                        };

                        actionRegistry?.Add(onSongChanged);
                        song.OnSongChanged += onSongChanged;
                        foreach (var trackFilter in trackSettings.TrackFilters)
                            trackFilter.OnSettingsChanged += onSongChanged;
                    }
                }
            }
            else
            {
                _tracks = new NativeArray<Track>(0, Allocator.Persistent);
            }

           
        }

        private NativeArray<byte> _disposed;

        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            var availableData = pipe.GetAvailableData(context);
            foreach (var element in availableData)
            {
                if (element.TryGetData(out PlaybackEvent playbackEvent))
                {
                    for (int i = 0; i < _tracks.Length; i++)
                    {
                        if (i != playbackEvent.TrackIndex) continue;
                        var track = _tracks[i];

                        track.HandlePlaybackEvent(playbackEvent);
                        _tracks[i] = track;
                    }
                }
            }
        }

        private uint seed;

        public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            uint state = seed;

            int NextInt(int min, int max)
            {
                if (min >= max) return min;
                state = state * 1103515245 + 12345;
                return min + (int)((state >> 16) % (uint)(max - min));
            }

            double sampleRate = _setup.sampleRate;
            double invSampleRate = 1.0 / sampleRate;
            double dspTime = context.dspTime * invSampleRate;
            
            
            int currentSub16Count = AnywhenAudioMetronome.SharedSub16Count.Data;
            if (currentSub16Count != _lastSub16Count)
            {
                
                _lastSub16Count = currentSub16Count;
                if (currentSub16Count % 4 == 0)
                {
                    var track = _tracks[0];
                    track.HandlePlaybackEvent(new PlaybackEvent(new SimpleNoteEvent(NextInt(0, 5)), dspTime, 0));
                    _tracks[0] = track;

                    seed = state;
                }

                if (currentSub16Count % 8 == 0)
                {
                    var track = _tracks[1];
                    track.HandlePlaybackEvent(new PlaybackEvent(new SimpleNoteEvent(NextInt(0, 5)), dspTime, 1));
                    _tracks[1] = track;
                }

                if (currentSub16Count % 2 == 0)
                {
                    var track = _tracks[2];
                    track.HandlePlaybackEvent(new PlaybackEvent(new SimpleNoteEvent(NextInt(0, 5)), dspTime, 1));
                    _tracks[2] = track;
                }
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
                out GeneratorInstance.Setup setup,
                ref GeneratorInstance.Properties p)
            {
                generator._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator._setup;
            }

            public void Dispose(ControlContext context, ref Processor generator)
            {
                if (generator._disposed.IsCreated)
                {
                    generator._disposed[0] = 1; // signal lambdas first
                    generator._disposed.Dispose();
                }

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

            // Add these two fields:
            private bool _hasPendingUpdate;
            private AnysongTrackSettings.Unmanaged _pendingSettings;

            public Track(int sampleRate, AnysongTrackSettings.Unmanaged settings) : this()
            {
                _sampleRate = sampleRate;
                UpdateSettings(settings);
            }

            public void OnValuesChanged(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingUpdate = true;
//                UpdateSettings(newSettings);
            }

            void UpdateSettings(AnysongTrackSettings.Unmanaged settings)
            {
                if (_voices.IsCreated) _voices.Dispose();
                if (_trackFilters.IsCreated) _trackFilters.Dispose();

                _voices = new NativeArray<SampleVoice>(settings.voices, Allocator.Persistent);
                _trackVolume = settings.volume;
                _sampleInstrument = settings.instrument;
                _trackEnvelope1 = new AudioProcessorEnvelope(_sampleRate);
                _trackEnvelope1.SetSettings(settings.TrackAudioEnvelope1.ToUnmanaged());


                _trackEnvelope2 = new AudioProcessorEnvelope(_sampleRate);
                _trackEnvelope2.SetSettings(settings.TrackAudioEnvelope2.ToUnmanaged());

                _trackEnvelope1Value = 1;
                _trackEnvelope2Value = 1;

                _trackLFO1 = new AudioProcessorLFO(_sampleRate);
                _trackLFO1.SetSettings(settings.TrackAudioLFO1.ToUnmanaged());
                _trackLFO2 = new AudioProcessorLFO(_sampleRate);
                _trackLFO2.SetSettings(settings.TrackAudioLFO2.ToUnmanaged());


                _trackLFO1Value = 0;
                _trackLFO2Value = 0;

                _nextEvent = new PlaybackEvent(new SimpleNoteEvent(), 0, 0);
                _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

                for (int i = 0; i < settings.trackFilters.Length; i++)
                {
                    _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
                }

                if (settings.trackFilters.IsCreated)
                {
                    settings.trackFilters.Dispose();
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
                        voice.QueueNote(playbackEvent.SimpleNoteEvent, playbackEvent.ScheduledPlayTime, ref _sampleInstrument);
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
                    voice.QueueNote(playbackEvent.SimpleNoteEvent, playbackEvent.ScheduledPlayTime, ref _sampleInstrument);
                    _voices[voiceToSteal] = voice;
                }
            }


            internal float Process(double dspTime)
            {
                // Apply pending update here — audio thread owns this Track, safe to dispose
                if (_hasPendingUpdate)
                {
                    UpdateSettings(_pendingSettings);
                    _hasPendingUpdate = false;
                }

                if (!_voices.IsCreated)
                {
                    return 0;
                }

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
                {
                    _voices.Dispose();
                }

                if (_trackFilters.IsCreated)
                {
                    _trackFilters.Dispose();
                }
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


            internal void QueueNote(SimpleNoteEvent noteEvent, double playTime,
                ref AnywhenSampleInstrument.Unmanaged sampleInstrument)
            {
                _noteQueued = true;
                _scheduledStartTime = playTime;
                var playbackSettings = sampleInstrument.GetNoteClipSettings(noteEvent.note);
                _clipData = playbackSettings.NoteClipUnmanaged;
                _pitch = playbackSettings.clipPitch;
                _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
                _samplePosition = 0;
            }


            public bool IsIdle => !_noteQueued;
            public double ScheduledStartTime => _scheduledStartTime;
        }
    }


    public struct PlaybackEvent
    {
        public SimpleNoteEvent SimpleNoteEvent;
        public readonly double ScheduledPlayTime;
        public readonly int TrackIndex;
        public readonly double ScheduledEndTime;

        public PlaybackEvent(SimpleNoteEvent simpleNoteEvent, double scheduledPlayTime, int trackIndex)
        {
            SimpleNoteEvent = simpleNoteEvent;
            ScheduledPlayTime = scheduledPlayTime;
            TrackIndex = trackIndex;
            ScheduledEndTime = ScheduledPlayTime + SimpleNoteEvent.duration;
        }
    }
}