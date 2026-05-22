using System;
using Anywhen;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "AnywhenAudioMetronome", menuName = "Anywhen/AnywhenAudioMetronome", order = 3)]
public class AnywhenAudioMetronome : ScriptableObject, IAudioGenerator
{
    public enum TickRate
    {
        None = 0,
        Sub2 = 2,
        Sub4 = 4,
        Sub8 = 8,
        Sub16 = 16,
        Bar = 0,
    }


    public static readonly SharedStatic<int> SharedSub16Count = SharedStatic<int>.GetOrCreate<AnywhenAudioMetronome>();
    public static readonly SharedStatic<int> SharedBarCount = SharedStatic<int>.GetOrCreate<AnywhenAudioMetronome, SharedBarCountKey>();
    public static readonly SharedStatic<double> SharedSub16Length = SharedStatic<double>.GetOrCreate<AnywhenAudioMetronome, SharedSub16LengthKey>();

    private struct SharedBarCountKey
    {
    }

    private struct SharedSub16LengthKey
    {
    }

    internal static readonly SharedStatic<double> SharedNextTime16 =
        SharedStatic<double>.GetOrCreate<AnywhenAudioMetronome, SharedNextTime16Key>();

    public static readonly SharedStatic<AnywhenProgressionPatternObject.Unmanaged>
        SharedBaseProgression = SharedStatic<AnywhenProgressionPatternObject.Unmanaged>.GetOrCreate<AnywhenAudioMetronome,
            SharedBaseProgressionStepKey>();

    public static readonly SharedStatic<AnywhenProgressionPatternObject.Unmanaged>
        SharedOverrideProgression = SharedStatic<AnywhenProgressionPatternObject.Unmanaged>
            .GetOrCreate<AnywhenAudioMetronome, SharedOverrideProgressionStepKey>();


    private struct SharedNextTime16Key
    {
    }

    private struct SharedBaseProgressionStepKey
    {
    }

    private struct SharedOverrideProgressionStepKey
    {
    }

    private GeneratorInstance _generatorInstance;
    [SerializeField] private int bpm = 120;
    public int Bpm => bpm;
    public static int CurrentBPM => AnywhenRuntime.Metronome.Bpm;

    public static int CurrentBar;
    
    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;
    public static Action OnTick16 { get; set; }
    public static Action OnTickSub16 { get; set; }
    public static Action OnTickSub8 { get; set; }
    public static Action OnTickSub4 { get; set; }
    public static Action OnTickSub2 { get; set; }
    public static Action OnTickBar { get; set; }


    public void Stop()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new StopMsg());
        }
    }

    public void Play()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new PlayMsg());
        }
    }

    private void Awake()
    {
        if (AnywhenRuntime.Metronome)
        {
            DestroyImmediate(this);
        }
    }

    public void SetBaseProgressionStep(NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> progressionSteps)
    {
        var newProgression = new AnywhenProgressionPatternObject.Unmanaged
        {
            patternSteps = progressionSteps
        };
        SharedBaseProgression.Data = newProgression;
    }

    public void SetOverrideProgressionStep(AnywhenProgressionPatternObject progression)
    {
        NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> progressionSteps =
            new NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged>(progression.patternSteps.Length,
                Allocator.Persistent);
        for (var i = 0; i < progressionSteps.Length; i++)
        {
            progressionSteps[i] = progression.patternSteps[i].ToUnmanaged();
        }

        var newProgression = new AnywhenProgressionPatternObject.Unmanaged
        {
            patternSteps = progressionSteps
        };
        SharedOverrideProgression.Data = newProgression;
    }

    public void SetTempo(int newTempo)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new SetBpmMsg(newTempo));
        }
        else
        {
            Debug.Log("No generator instance");
        }
    }

    public void Restart()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new RestartMsg());
        }
    }

    public static float GetTimeToNext16()
    {
        double timeToNext = SharedNextTime16.Data - AudioSettings.dspTime;
        return timeToNext > 0 ? (float)timeToNext : 0f;
    }

    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        _generatorInstance = Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, bpm);
        return _generatorInstance;
    }


    public void Reset()
    {
        Debug.Log("Destroying metronome");
        SharedOverrideProgression.Data.patternSteps =
            new NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged>(0, Allocator.Persistent);
        SharedOverrideProgression.Data = default;
        SharedOverrideProgression.Data = new AnywhenProgressionPatternObject.Unmanaged();
        Stop();
    }

    public class SetBpmMsg
    {
        public int NewBpm;

        public SetBpmMsg(int newBpm)
        {
            NewBpm = newBpm;
        }
    }

    public class RestartMsg
    {
    }

    public class StopMsg
    {
    }

    public class PlayMsg
    {
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct Processor : GeneratorInstance.IRealtime
    {
        private int _bpm;
        private double _sampleRate;
        private double _invSampleRate;
        private double _sub16Length;
        public static double Sub16Length => SharedSub16Length.Data;
        private double _nextTime16;
        private int _sub16Count;
        private bool _isPlaying;

        private GeneratorInstance.Setup _setup;

        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, int bpm)
        {
            return context.AllocateGenerator(new Processor(sampleRate, bpm), new Control());
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;

        private Processor(int sampleRate, int bpm)
        {
            _sampleRate = sampleRate;
            _invSampleRate = 1.0 / _sampleRate;
            _bpm = bpm;
            _setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, sampleRate);

            _sub16Length = (60.0 / _bpm) * 0.25;
            AnywhenAudioMetronome.SharedSub16Length.Data = _sub16Length;
            _nextTime16 = -1;
            _sub16Count = 0;
            _isPlaying = true;
        }

        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            var availableData = pipe.GetAvailableData(context);
            foreach (var element in availableData)
            {
                if (element.TryGetData(out BPMState data))
                {
                    _bpm = data.bpm;
                    _sub16Length = (60.0 / _bpm) * 0.25f;
                    AnywhenAudioMetronome.SharedSub16Length.Data = _sub16Length;
                }

                if (element.TryGetData(out RestartState _))
                {
                    SharedBarCount.Data = 0;
                    SharedSub16Count.Data = -1;
                    _nextTime16 = -1;
                    _sub16Count = -1;
                }

                if (element.TryGetData(out PlayState playState))
                {
                    _isPlaying = playState.isPlaying;
                    if (_isPlaying)
                    {
                        SharedBarCount.Data = 0;
                        SharedSub16Count.Data = -1;
                        _nextTime16 = -1;
                        _sub16Count = -1;
                    }
                }
            }
        }

        public GeneratorInstance.Result Process(in RealtimeContext ctx, ProcessorInstance.Pipe pipe, ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            if (!_isPlaying)
            {
                return buffer.frameCount;
            }

            if (_nextTime16 < 0)
            {
                _nextTime16 = (double)ctx.dspTime * _invSampleRate;
            }

            double endDspTime = ((double)ctx.dspTime + (double)buffer.frameCount) * _invSampleRate;

            while (_nextTime16 < endDspTime)
            {
                EmitTicks(ctx, pipe);
                _sub16Count++;
                if (_sub16Count >= 16)
                {
                    _sub16Count = 0;
                    SharedBarCount.Data++;
                }

                _nextTime16 += _sub16Length;

                SharedSub16Count.Data = _sub16Count;
                SharedNextTime16.Data = _nextTime16;
            }

            return buffer.frameCount;
        }


        public static void SetBaseProgression(NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> progression)
        {
            var newProgression = new AnywhenProgressionPatternObject.Unmanaged
            {
                patternSteps = progression
            };
            SharedBaseProgression.Data = newProgression;
        }

        public static void SetOverrideProgression(
            NativeArray<AnywhenProgressionPatternObject.ProgressionStep.Unmanaged> progression)
        {
            var newProgression = new AnywhenProgressionPatternObject.Unmanaged
            {
                patternSteps = progression
            };
            SharedOverrideProgression.Data = newProgression;
        }

        public static int GetScaledNote(int noteStep)
        {
            var overrideProgression = SharedOverrideProgression.Data;
            if (overrideProgression.patternSteps.IsCreated && overrideProgression.patternSteps.Length > 0)
            {
                var steps = overrideProgression.patternSteps;
                return GetScaledNote(steps[SharedBarCount.Data % steps.Length], noteStep);
            }

            var baseProgression = SharedBaseProgression.Data;
            if (baseProgression.patternSteps.IsCreated && baseProgression.patternSteps.Length > 0)
            {
                var steps = baseProgression.patternSteps;
                return GetScaledNote(steps[SharedBarCount.Data % steps.Length], noteStep);
            }

            return 0;
        }

        static int GetScaledNote(AnywhenProgressionPatternObject.ProgressionStep.Unmanaged progressionStep, int noteStep)
        {
            if (progressionStep.AnywhenScale.IsNull())
            {
                Debug.LogError("No scale set!");
                return 0;
            }

            if (progressionStep.AnywhenScale.notes.Length == 0)
            {
                Debug.LogError("No notes in scale!");
                return 0;
            }

            int numNotes = progressionStep.AnywhenScale.notes.Length;
            int octave = Mathf.FloorToInt((float)noteStep / numNotes);
            int noteIndex = noteStep % numNotes;
            if (noteIndex < 0)
            {
                noteIndex += numNotes;
            }

            int returnNote = progressionStep.AnywhenScale.notes[noteIndex] + (octave * 12);
            return returnNote + progressionStep.RootNote;
        }


        private void EmitTicks(RealtimeContext context, ProcessorInstance.Pipe pipe)
        {
            MetronomeTickEvent e = new MetronomeTickEvent
            {
                TickRate = TickRate.Sub16,
                Count = _sub16Count
            };
            pipe.SendData(context, e);

            if (_sub16Count == 0)
            {
                e.TickRate = TickRate.Bar;
                e.Count = 0;
                pipe.SendData(context, e);
            }


            if (_sub16Count % 2 == 0)
            {
                e.TickRate = TickRate.Sub8;
                e.Count = _sub16Count / 2;
                pipe.SendData(context, e);
            }

            if (_sub16Count % 4 == 0)
            {
                e.TickRate = TickRate.Sub4;
                e.Count = _sub16Count / 4;
                pipe.SendData(context, e);
            }

            if (_sub16Count % 8 == 0)
            {
                e.TickRate = TickRate.Sub2;
                e.Count = _sub16Count / 8;
                pipe.SendData(context, e);
            }
        }

        private struct Control : GeneratorInstance.IControl<Processor>
        {
            public void Configure(ControlContext context, ref Processor generator, in AudioFormat config,
                out GeneratorInstance.Setup setup,
                ref GeneratorInstance.Properties p)
            {
                generator._sampleRate = config.sampleRate;
                generator._invSampleRate = 1.0 / generator._sampleRate;
                generator._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator._setup;
            }

            public void Dispose(ControlContext context, ref Processor generator)
            {
            }

            public void Update(ControlContext context, ProcessorInstance.Pipe pipe)
            {
                // Managed side — safe to call delegates and invoke Actions here
                var availableData = pipe.GetAvailableData(context);
                foreach (var element in availableData)
                {
                    if (element.TryGetData(out MetronomeTickEvent tick))
                    {
                        switch (tick.TickRate)
                        {
                            case TickRate.Sub16:
                                OnTickSub16?.Invoke();
                                break;
                            case TickRate.Sub8:
                                OnTickSub8?.Invoke();
                                break;
                            case TickRate.Sub4:
                                OnTickSub4?.Invoke();
                                break;
                            case TickRate.Sub2:
                                OnTickSub2?.Invoke();
                                break;
                            case TickRate.Bar:
                                OnTickBar?.Invoke();
                                break;
                        }
                    }
                }
            }

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe,
                ProcessorInstance.Message message)
            {
                if (message.Is<SetBpmMsg>())
                {
                    var payload = message.Get<SetBpmMsg>();
                    pipe.SendData(context, new BPMState { bpm = payload.NewBpm });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<RestartMsg>())
                {
                    pipe.SendData(context, new RestartState());
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<StopMsg>())
                {
                    pipe.SendData(context, new PlayState { isPlaying = false });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<PlayMsg>())
                {
                    pipe.SendData(context, new PlayState { isPlaying = true });
                    return ProcessorInstance.Response.Handled;
                }

                return ProcessorInstance.Response.Unhandled;
            }
        }
    }

    public struct BPMState
    {
        public int bpm;
    }

    public struct RestartState
    {
    }

    public struct PlayState
    {
        public bool isPlaying;
    }

  
}

public struct MetronomeTickEvent
{
    public AnywhenAudioMetronome.TickRate TickRate;
    public int Count;
}