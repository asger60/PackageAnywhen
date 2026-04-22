using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AnywhenAudioPlayer", menuName = "Sample/Create AnywhenAudioPlayer asset", order = 2)]
public class AnywhenAudioGenrator : ScriptableObject, IAudioGenerator
{
    [SerializeField] AnysongObject song;
    
    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;

    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        return Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, song);
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

    public class TriggerSetupMsg
    {
        public TriggerSetupMsg(AnysongObject trackSettings)
        {
            SetupEvent = new SetupEvent(trackSettings);
        }

        public SetupEvent SetupEvent { get; }
    }


    [BurstCompile(CompileSynchronously = true)]
    private struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<Track> _tracks;
        int _sampleRate;
        GeneratorInstance.Setup _setup;

        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, AnysongObject initialSong = null)
        {
            return context.AllocateGenerator(new Processor(sampleRate, initialSong), new Control());
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;


        Processor(int sampleRate, AnysongObject initialSong = null)
        {
            _sampleRate = sampleRate;
            _setup = new GeneratorInstance.Setup();
            if (initialSong != null)
            {
                _tracks = new NativeArray<Track>(initialSong.Tracks.Count, Allocator.Persistent);
                for (int i = 0; i < _tracks.Length; i++)
                {
                    var trackSettings = initialSong.Tracks[i];
                    if (trackSettings.instrument is AnywhenSampleInstrument sampleInstrument)
                    {
                        int voiceCount = trackSettings.voices;
                        _tracks[i] = new Track(voiceCount, _sampleRate, trackSettings.volume,
                            sampleInstrument.ToUnmanaged());
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
            var availableData = pipe.GetAvailableData(context);
            foreach (var element in availableData)
            {
                if (element.TryGetData(out PlaybackEvent playbackEvent))
                {
                    for (int i = 0; i < _tracks.Length; i++)
                    {
                        if (i != playbackEvent.TrackIndex) continue;
                        var track = _tracks[i];
                        Debug.Log("track hash: " + track.GetHashCode() + " clip selevt type " + track.Instrument.clipSelectType);

                        track.HandlePlaybackEvent(playbackEvent);
                        _tracks[i] = track;
                    }
                }

                if (element.TryGetData(out SetupEvent setupEvent))
                {
                    Debug.Log("Setting up with " + setupEvent.TrackSettings.Length + " tracks");

                    if (_tracks.IsCreated)
                    {
                        for (int i = 0; i < _tracks.Length; i++)
                        {
                            Debug.Log("Disposing track " + i);
                            _tracks[i].Dispose();
                        }

                        Debug.Log("Disposing tracks");
                        _tracks.Dispose();
                    }

                    _tracks = new NativeArray<Track>(setupEvent.TrackSettings.Length, Allocator.Persistent);
                    Debug.Log("Created tracks " + _tracks.Length + " with sample rate " + _sampleRate + " and " + setupEvent.TrackSettings.Length +
                              " tracks");
                    for (int i = 0; i < _tracks.Length; i++)
                    {
                        var sampleInstrument = setupEvent.TrackSettings[i].instrument;
                        int voiceCount = setupEvent.TrackSettings[i].voices;
                        if (voiceCount <= 0)
                        {
                            voiceCount = 1;
                        }

                        _tracks[i] = new Track(voiceCount, _sampleRate, setupEvent.TrackSettings[i].volume, sampleInstrument);
                    }
                }
            }
        }

        public GeneratorInstance.Result Process(in RealtimeContext ctx, ProcessorInstance.Pipe pipe, ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            double dspTime = ctx.dspTime;
            double sampleRate = _setup.sampleRate;
            double invSampleRate = 1.0 / sampleRate;

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
            public void Configure(ControlContext context, ref Processor generator, in AudioFormat config, out GeneratorInstance.Setup setup,
                ref GeneratorInstance.Properties p)
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

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe, ProcessorInstance.Message message)
            {
                if (message.Is<TriggerNoteClipMsg>())
                {
                    var payload = message.Get<TriggerNoteClipMsg>();
                    pipe.SendData(context, payload.PlaybackEvent);
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerSetupMsg>())
                {
                    var payload = message.Get<TriggerSetupMsg>();
                    pipe.SendData(context, payload.SetupEvent);
                    return ProcessorInstance.Response.Handled;
                }

                return ProcessorInstance.Response.Unhandled;
            }
        }


        struct Track
        {
            private NativeArray<SampleVoice> _voices;
            private AnywhenSampleInstrument.Unmanaged _sampleInstrument;
            public AnywhenSampleInstrument.Unmanaged Instrument => _sampleInstrument;

            private float _trackVolume;
            private ADSR _trackEnvelope;


            public Track(int voices, int sampleRate, float trackVolume, AnywhenSampleInstrument.Unmanaged instrument)
            {
                _voices = new NativeArray<SampleVoice>(voices, Allocator.Persistent);
                for (var i = 0; i < _voices.Length; i++)
                {
                    _voices[i].Init(sampleRate);
                }

                _trackVolume = trackVolume;
                _sampleInstrument = instrument;
                _trackEnvelope = new ADSR();
                _trackEnvelope.Init();
                _nextEvent = new PlaybackEvent(new SimpleNoteEvent(), 0, 0);
                Debug.Log("Created track with " + _voices.Length + " voices and instrument " + _sampleInstrument.seed + " select mode: " +
                          _sampleInstrument.clipSelectType + "");
                Debug.Log("Track hash: " + GetHashCode());
            }


            private PlaybackEvent _nextEvent;

            internal void HandlePlaybackEvent(PlaybackEvent playbackEvent)
            {
                Debug.Log("Received playback event " + _voices.Length + " voices");
                Debug.Log("samele instrunment " + _sampleInstrument.seed);

                _nextEvent = playbackEvent;
                for (int i = 0; i < _voices.Length; i++)
                {
                    var voice = _voices[i];
                    if (voice.IsIdle)
                    {
                        Debug.Log("found idle voice, queuing note");
                        voice.QueueNote(playbackEvent.SimpleNoteEvent, playbackEvent.ScheduledPlayTime, ref _sampleInstrument);
                        _voices[i] = voice;
                        break;
                    }

                    _voices[i] = voice;
                }
            }

            internal float Process(double dspTime)
            {
                if (!_voices.IsCreated)
                {
                    return 0;
                }

                if (dspTime >= _nextEvent.ScheduledPlayTime && dspTime < _nextEvent.ScheduledEndTime)
                {
                    _trackEnvelope.SetGate(true);
                }

                if (dspTime >= _nextEvent.ScheduledEndTime)
                {
                    _trackEnvelope.SetGate(false);
                }

                float clipAmplitude = 0;
                for (int i = 0; i < _voices.Length; i++)
                {
                    var voice = _voices[i];
                    clipAmplitude += voice.Process(dspTime);
                    _voices[i] = voice;
                }

                return clipAmplitude * _trackVolume /* * _trackEnvelope.Process()*/;
            }


            public void Dispose()
            {
                if (_voices.IsCreated)
                {
                    _voices.Dispose();
                }
            }
        }

        private struct SampleVoice
        {
            private double _scheduledStartTime;

            private AnywhenNoteClip.Unmanaged _clipData;

            private int _sampleCount;
            private int _sampleIndex;
            private bool _noteOn;
            private bool _noteQueued;
            private int _sampleRate;

            internal float Process(double dspTime)
            {
                float clipAmplitude = 0;
                if (_noteQueued && dspTime >= _scheduledStartTime)
                {
                    if (_clipData.clipSamples.IsCreated && _sampleIndex < _sampleCount)
                    {
                        int channels = _clipData.channels;
                        for (int c = 0; c < channels && _sampleIndex < _sampleCount; c++)
                        {
                            clipAmplitude += _clipData.clipSamples[_sampleIndex++];
                        }

                        if (channels > 1)
                            clipAmplitude /= channels;
                    }
                    else
                    {
                        _noteQueued = false;
                        Dispose();
                    }
                }


                return clipAmplitude /* * _clipData.Volume*/;
            }


            internal void QueueNote(SimpleNoteEvent noteEvent, double playTime, ref AnywhenSampleInstrument.Unmanaged sampleInstrument)
            {
                _noteQueued = true;
                _scheduledStartTime = playTime;
                Debug.Log("sample instrument " + sampleInstrument.seed + " " +
                          sampleInstrument.GetNoteClipSettings(noteEvent.note, ref sampleInstrument.seed).noteClip.ToUnmanaged().clipSamples.Length + " samples");

                _clipData = sampleInstrument.GetNoteClipSettings(noteEvent.note, ref sampleInstrument.seed).noteClip.ToUnmanaged();

                _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
                _sampleIndex = 0;
                Debug.Log("Queued note " + noteEvent.note + " " + _sampleCount + " samples. Instrument seed: " + sampleInstrument.seed);
            }


            private void Dispose()
            {
            }

            public bool IsIdle => !_noteQueued;

            public void Init(int sampleRate)
            {
                _sampleRate = sampleRate;
            }
        }
    }

    public struct SampleInstrument
    {
        internal NativeArray<NoteClipData> NoteClips;

        public SampleInstrument(AnywhenSampleInstrument sampleInstrument)
        {
            NoteClips = new NativeArray<NoteClipData>(sampleInstrument.clipDatas.Length, Allocator.Persistent);
            for (int i = 0; i < sampleInstrument.clipDatas.Length; i++)
            {
                NoteClips[i] = new NoteClipData(InstrumentDatabase.GetNoteClips(sampleInstrument)[i].clipSamples, 1, 1,
                    sampleInstrument.clipDatas[i].noteIndex);
            }
        }

        internal NoteClipData GetNoteClip(int noteIndex)
        {
            foreach (var noteClip in NoteClips)
            {
                if (noteClip.NoteIndex == noteIndex)
                    return noteClip;
            }

            return NoteClips[0];
        }
    }

    public struct SetupEvent
    {
        public NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;

        public SetupEvent(AnysongObject anysongObject) : this()
        {
            TrackSettings = new NativeArray<AnysongTrackSettings.Unmanaged>(anysongObject.Tracks.Count, Allocator.Persistent);
            for (int i = 0; i < TrackSettings.Length; i++)
            {
                TrackSettings[i] = anysongObject.Tracks[i].ToUnmanaged();
            }
        }
    }

    public struct PlaybackEvent
    {
        public SimpleNoteEvent SimpleNoteEvent;
        public readonly double ScheduledPlayTime;
        public readonly int TrackIndex;
        public double ScheduledEndTime => ScheduledPlayTime + SimpleNoteEvent.duration;

        public PlaybackEvent(SimpleNoteEvent simpleNoteEvent, double scheduledPlayTime, int trackIndex)
        {
            SimpleNoteEvent = simpleNoteEvent;
            ScheduledPlayTime = scheduledPlayTime;
            TrackIndex = trackIndex;
        }
    }

    public struct NoteClipData
    {
        public NativeArray<float> Samples;
        public readonly int Channels;
        public float Volume;
        public float Pitch;
        public int NoteIndex;


        public NoteClipData(float[] samples, int channels, float volume, int noteIndex)
        {
            Samples = new NativeArray<float>(samples, Allocator.Persistent);
            Channels = channels;
            Volume = volume;
            Pitch = 1;
            NoteIndex = noteIndex;
        }
    }
}