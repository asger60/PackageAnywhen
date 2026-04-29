using System;
using Unity.Burst;
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
    }

    public delegate void MetronomeTickDelegate(MetronomeTickEvent tick);

    public static readonly SharedStatic<int> SharedSub16Count = SharedStatic<int>.GetOrCreate<AnywhenAudioMetronome>();
    private GeneratorInstance _generatorInstance;
    [SerializeField] private int bpm = 120;
    public static int CurrentBPM;
    public int Bpm => bpm;

    public static event MetronomeTickDelegate OnAudioTick;

    internal static void TriggerTick(MetronomeTickEvent tick)
    {
        OnAudioTick?.Invoke(tick);
    }

    public static float Sub16Length => (60f / CurrentBPM) * 0.25f;

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;
    public static Action OnTick16 { get; set; }
    public static Action OnTickSub16 { get; set; }
    public static Action OnTickSub8 { get; set; }
    public static Action OnTickSub4 { get; set; }
    public static Action OnTickSub2 { get; set; }
    public static Action OnBar { get; set; }

    

    public void SetTempo(int newBPM)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new SetBpmMsg(newBPM));
        }
    }
    
    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        _generatorInstance = Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, bpm);
        return _generatorInstance;
    }

    public class SetBpmMsg
    {
        public int NewBpm;

        public SetBpmMsg(int newBpm)
        {
            NewBpm = newBpm;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct Processor : GeneratorInstance.IRealtime
    {
        private int _bpm;
        private double _sampleRate;
        private double _invSampleRate;
        private double _sub16Length;
        private double _nextTime16;
        private int _sub16Count;

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
            _nextTime16 = -1;
            _sub16Count = 0;
        }

        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            // Only handle blittable BPM updates here — no managed delegate calls allowed in Burst
            var availableData = pipe.GetAvailableData(context);
            foreach (var element in availableData)
            {
                if (element.TryGetData(out BPMState data))
                {

                    CurrentBPM = data.bpm;
                    _bpm = data.bpm;
                    _sub16Length = (60.0 / _bpm) * 0.25f;
                }

            }
        }

        public GeneratorInstance.Result Process(in RealtimeContext ctx, ProcessorInstance.Pipe pipe, ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            if (_nextTime16 < 0)
            {
                _nextTime16 = (double)ctx.dspTime * _invSampleRate;
            }

            double endDspTime = ((double)ctx.dspTime + (double)buffer.frameCount) * _invSampleRate;

            while (_nextTime16 < endDspTime)
            {
                EmitTicks(ctx, pipe);

                _sub16Count++;
                if (_sub16Count >= 16) _sub16Count = 0;
                _nextTime16 += _sub16Length;

                SharedSub16Count.Data = _sub16Count;
            }

            return buffer.frameCount;
        }

        private void EmitTicks(RealtimeContext context, ProcessorInstance.Pipe pipe)
        {
            MetronomeTickEvent e = new MetronomeTickEvent
            {
                TickRate = TickRate.Sub16,
                Count = _sub16Count
            };
            pipe.SendData(context, e);

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
                        TriggerTick(tick);

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
                                //if (tick.Count == 0) OnBar?.Invoke();
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

                return ProcessorInstance.Response.Unhandled;
            }
        }
    }

    public struct BPMState
    {
        public int bpm;
    }

 
}

public struct MetronomeTickEvent
{
    public AnywhenAudioMetronome.TickRate TickRate;
    public int Count;
}