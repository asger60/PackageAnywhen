using System;
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
public class AnywhenAudioGenerator : ScriptableObject, IAudioGenerator
{
    [SerializeField] AnysongObject song;

    private bool _sectionLockState;
    int _currentLockSectionIndex;
    private bool _isPlaying;
    private GeneratorInstance _generatorInstance;

    private void OnEnable()
    {
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

    public void SetIntensity(float value)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerIntensityMsg(value));
        }
    }

    public void SetSnapshot(float testSnapshot)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerSnapshotMsg(testSnapshot));
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
        return _generatorInstance;
    }


    public class TriggerPlaybackMsg
    {
        public readonly bool isPlaying;

        public TriggerPlaybackMsg(bool isPlaying)
        {
            this.isPlaying = isPlaying;
        }
    }

    public class TriggerIntensityMsg
    {
        public float intensity;

        public TriggerIntensityMsg(float intensity)
        {
            this.intensity = intensity;
        }
    }

    public class TriggerSnapshotMsg
    {
        public float snapshotValue;

        public TriggerSnapshotMsg(float snapshotValue)
        {
            this.snapshotValue = snapshotValue;
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
        private bool _isPlaying;
        private float _intensity;

        private float _currentSnapshotValue;

        //private AnysongObject _currentSong;
        private uint seed;
        int _currentSectionIndex;

        Processor(int sampleRate, AnysongObject song, NativeArray<int> stepIndices)
        {
            _stepIndices = stepIndices; // <-- assign
            seed = 12345;
            _sampleRate = sampleRate;
            _setup = new GeneratorInstance.Setup();
            _selfHandle = default;
            _lastSub16Count = -1;
            _anysongSections = default;
            _isPlaying = false;
            _currentSectionIndex = 0;
            _intensity = 1;
            _currentSnapshotValue = 0;
            //_currentSong = song;
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
                    var track = section.Tracks[trackIndex];
                    var pattern = track.Patterns[patternIndex];
                    var previousInternalIndex = track.Patterns[patternIndex].internalIndex;
                    pattern = song.Sections[sectionIndex].tracks[trackIndex].patterns[patternIndex].ToUnmanaged();
                    pattern.internalIndex = previousInternalIndex;
                    track.Patterns[patternIndex] = pattern;

                    section.Tracks[trackIndex] = track;
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
                            t.OnEffectsRebuild(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };

                        Action tracksListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnTracksRebuild(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };

                        song.OnSongSettingsChanged += settingsListener;
                        song.OnSongEffectsChanged += effectsListener;
                        song.OnSongTracksChanged += tracksListener;
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
                        for (int trackIndex = 0; trackIndex < _anysongSections[0].Tracks.Length; trackIndex++)
                        {
                            var section = _anysongSections[0];
                            var track = section.Tracks[trackIndex];
                            track.Reset();
                            var pattern = track.Patterns[track.CurrentPatternIndex];
                            pattern.SyncToMetronome(AnywhenAudioMetronome.SharedSub16Count.Data);
                            track.Patterns[track.CurrentPatternIndex] = pattern;
                            section.Tracks[trackIndex] = track;
                        }
                    }
                }

                if (element.TryGetData(out IntensityData intensityData))
                {
                    _intensity = intensityData.Intensity;
                }

                if (element.TryGetData(out SnapshotData snapshotData))
                {
                    _currentSnapshotValue = snapshotData.SnapshotValue;
                    Debug.Log($"Snapshot value: {_currentSnapshotValue}");
                    //AnywhenSnapshotBlender.ApplyBlend(_currentSong, _currentSnapshotValue);
                    //_currentSong.RefreshSettings();
                }
            }
        }


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

            if (_isPlaying && currentSub16Count != _lastSub16Count)
            {
                if (currentSub16Count == 0)
                {
                    int prevIndex = _currentSectionIndex;
                    var section = _anysongSections[prevIndex];

                    section.AdvancePlayingSection();
                    if (section.IsComplete())
                    {
                        section.Reset();
                        _currentSectionIndex = (_currentSectionIndex + 1) % _anysongSections.Length;
                    }

                    _anysongSections[prevIndex] = section;
                }

                for (int trackIndex = 0; trackIndex < _anysongSections[_currentSectionIndex].Tracks.Length; trackIndex++)
                {
                    var section = _anysongSections[_currentSectionIndex];
                    var track = section.Tracks[trackIndex];
                    if (currentSub16Count == 0)
                    {
                        track.AdvancePlayingPattern();
                    }

                    var pattern = track.Patterns[track.CurrentPatternIndex];


                    if (_stepIndices.IsCreated && trackIndex < _stepIndices.Length)
                    {
                        _stepIndices[trackIndex] = pattern.internalIndex;
                    }


                    foreach (var thisNote in pattern.GetCurrentStep().StepNotes)
                    {
                        bool chancePass = thisNote.chance * 100 > NextInt(0, 100);
                        bool intensityPass = thisNote.mixWeight > (1 - _intensity);

                        if (chancePass && intensityPass)
                        {
                            var playbackTrack = _tracks[trackIndex];
                            playbackTrack.HandlePlaybackEvent(new PlaybackEvent(thisNote, dspTime));
                            _tracks[trackIndex] = playbackTrack;
                        }
                    }


                    pattern.AdvancePlayingStep();
                    track.Patterns[track.CurrentPatternIndex] = pattern;
                    section.Tracks[trackIndex] = track;
                    _anysongSections[_currentSectionIndex] = section;
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

            seed = state;
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

                if (message.Is<TriggerIntensityMsg>())
                {
                    var payload = message.Get<TriggerIntensityMsg>();
                    pipe.SendData(context, new IntensityData { Intensity = payload.intensity });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerSnapshotMsg>())
                {
                    var payload = message.Get<TriggerSnapshotMsg>();
                    pipe.SendData(context, new SnapshotData { SnapshotValue = payload.snapshotValue });
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
            public AnywhenSampleInstrument.Unmanaged SampleInstrument => _sampleInstrument;
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
            public AudioProcessorSettings.EnvelopeSettings.Unmanaged EnvelopeSettings => _settings.TrackAudioEnvelope1.ToUnmanaged();

            private bool _hasPendingTracksUpdate;
            private bool _hasPendingEffectsUpdate;
            private bool _hasPendingParameterUpdate;
            private AnysongTrackSettings.Unmanaged _pendingSettings;
            AnysongTrackSettings.Unmanaged _settings;

            public Track(int sampleRate, AnysongTrackSettings.Unmanaged settings) : this()
            {
                _sampleRate = sampleRate;
                _trackLFO1Value = 0;
                _trackLFO2Value = 0;

                CreateTrack(settings, sampleRate);
                UpdateSettings(settings);
                _nextEvent = new PlaybackEvent(new SimpleNoteEvent(), 0);
            }

            public void OnTracksRebuild(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingTracksUpdate = true;
            }

            public void OnEffectsRebuild(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingEffectsUpdate = true;
                _hasPendingParameterUpdate = true;
            }

            public void OnValuesChanged(AnysongTrackSettings.Unmanaged newSettings)
            {
                _pendingSettings = newSettings;
                _hasPendingParameterUpdate = true;
            }

            void CreateTrack(AnysongTrackSettings.Unmanaged settings, int sampleRate)
            {
                if (_voices.IsCreated) _voices.Dispose();
                if (_trackFilters.IsCreated) _trackFilters.Dispose();
                _voices = new NativeArray<SampleVoice>(settings.voices, Allocator.Persistent);
                for (int i = 0; i < _voices.Length; i++)
                {
                    var voice = _voices[i];
                    voice.Init(sampleRate);
                    _voices[i] = voice;
                }

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

            private void CreateEffects(AnysongTrackSettings.Unmanaged settings)
            {
                _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

                for (int i = 0; i < settings.trackFilters.Length; i++)
                {
                    _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
                }
            }

            void UpdateSettings(AnysongTrackSettings.Unmanaged settings)
            {
                _settings = settings;
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

                if (_voices.IsCreated)
                {
                    for (int i = 0; i < _voices.Length; i++)
                    {
                        var voice = _voices[i];
                        voice.UpdateVoiceSettings(_sampleInstrument, settings.TrackAudioEnvelope1.ToUnmanaged(), settings.audioSourceType,
                            settings.synthOscillatorType);
                        _voices[i] = voice;
                    }
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
                        voice.QueueNote(playbackEvent);
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
                    voice.QueueNote(playbackEvent);
                    _voices[voiceToSteal] = voice;
                }
            }

            internal float Process(double dspTime)
            {
                if (_hasPendingTracksUpdate)
                {
                    CreateTrack(_pendingSettings, _sampleRate);
                    _hasPendingTracksUpdate = false;
                }

                if (_hasPendingEffectsUpdate)
                {
                    CreateEffects(_pendingSettings);
                    _hasPendingEffectsUpdate = false;
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
                    clipAmplitude += voice.Process(dspTime, this);
                    _voices[i] = voice;
                }

                //clipAmplitude *= TrackEnvelope1Value;

                if (_trackFilters.IsCreated)
                {
                    for (int i = 0; i < _trackFilters.Length; i++)
                    {
                        var filter = _trackFilters[i];
                        clipAmplitude = filter.Process(clipAmplitude, this);
                        _trackFilters[i] = filter;
                    }
                }

                return clipAmplitude * _trackVolume;
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
            private double _scheduledStartTime, _scheduledEndTime;
            private AnywhenNoteClip.Unmanaged _clipData;
            private int _sampleCount;
            private double _samplePosition;
            private bool _noteOn;
            private bool _noteQueued;
            private float _pitch;
            private float _velocity;
            AudioProcessorEnvelope _voiceEnvelope;
            private AnywhenSampleInstrument.Unmanaged _sampleInstrument;
            private AudioProcessorSettings.EnvelopeSettings.Unmanaged _envelopeSettings;
            private AnysongTrackSettings.AudioSourceType _audioSourceType;
            private AnysongTrackSettings.SynthOscillatorTypes _oscillatorType;

            public void Init(int sampleRate)
            {
                _voiceEnvelope = new AudioProcessorEnvelope(sampleRate);
            }

            public void UpdateVoiceSettings(
                AnywhenSampleInstrument.Unmanaged sampleInstrument,
                AudioProcessorSettings.EnvelopeSettings.Unmanaged envelopeSettings,
                AnysongTrackSettings.AudioSourceType audioSourceType,
                AnysongTrackSettings.SynthOscillatorTypes oscillatorType)
            {
                _sampleInstrument = sampleInstrument;
                _envelopeSettings = envelopeSettings;
                _audioSourceType = audioSourceType;
                _oscillatorType = oscillatorType;
            }

            internal float Process(double dspTime, in Track track)
            {
                float clipAmplitude = 0;
                if (_noteQueued && dspTime >= _scheduledStartTime)
                {
                    switch (_audioSourceType)
                    {
                        case AnysongTrackSettings.AudioSourceType.Sample:
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

                            break;
                        case AnysongTrackSettings.AudioSourceType.Synth:
                            switch (_oscillatorType)
                            {
                                case AnysongTrackSettings.SynthOscillatorTypes.Sine:

                                    break;
                                case AnysongTrackSettings.SynthOscillatorTypes.Saw:
                                    break;
                                case AnysongTrackSettings.SynthOscillatorTypes.Square:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _voiceEnvelope.SetGate(dspTime >= _scheduledStartTime && dspTime <= _scheduledEndTime);

                return clipAmplitude * _velocity * _voiceEnvelope.Process((float)dspTime, track);
            }

            internal void QueueNote(PlaybackEvent playbackEvent)
            {
                _voiceEnvelope.SetSettings(_envelopeSettings);
                _voiceEnvelope.Reset();
                _noteQueued = true;
                _scheduledStartTime = playbackEvent.ScheduledPlayTime;
                _scheduledEndTime = playbackEvent.ScheduledEndTime;
                var clipSettings = _sampleInstrument.GetNoteClipSettings(playbackEvent.Note.noteIndex);
                _clipData = clipSettings.NoteClipUnmanaged;
                _pitch = clipSettings.clipPitch;
                _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
                _samplePosition = 0;
                _velocity = playbackEvent.Note.velocity;
            }

            public bool IsIdle => !_noteQueued;
            public double ScheduledStartTime => _scheduledStartTime;
        }
    }


    public struct PlaybackEvent : IEquatable<PlaybackEvent>
    {
        public AnysongPatternNote Note;
        public readonly double ScheduledPlayTime;
        public readonly double ScheduledEndTime;

        public PlaybackEvent(AnysongPatternNote note, double scheduledPlayTime)
        {
            Note = note;
            ScheduledPlayTime = scheduledPlayTime + Note.drift * AnywhenAudioMetronome.Sub16Length;
            ScheduledEndTime = scheduledPlayTime + Note.duration +
                               Note.drift * AnywhenAudioMetronome.Sub16Length;
        }

        public PlaybackEvent(SimpleNoteEvent simpleNoteEvent, double scheduledPlayTime)
        {
            Note = new AnysongPatternNote(simpleNoteEvent.note);
            ScheduledPlayTime = scheduledPlayTime + simpleNoteEvent.drift;
            ScheduledEndTime = ScheduledPlayTime + Note.duration + simpleNoteEvent.drift;
        }

        public bool Equals(PlaybackEvent other)
        {
            return Note.Equals(other.Note) && ScheduledPlayTime.Equals(other.ScheduledPlayTime) &&
                   ScheduledEndTime.Equals(other.ScheduledEndTime);
        }

        public override bool Equals(object obj)
        {
            return obj is PlaybackEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Note, ScheduledPlayTime, ScheduledEndTime);
        }
    }

    public struct PlaybackStateData
    {
        public bool IsPlaying;
    }

    public struct IntensityData
    {
        public float Intensity;
    }

    public struct SnapshotData
    {
        public float SnapshotValue;
    }
}