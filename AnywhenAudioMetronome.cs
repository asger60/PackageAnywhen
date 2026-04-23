using Anywhen;
using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "AnywhenAudioMetronome", menuName = "Anywhen/AnywhenAudioMetronome", order = 3)]
public class AnywhenAudioMetronome : ScriptableObject, IAudioGenerator
{
    public delegate void MetronomeTickDelegate(MetronomeTickEvent tick);


    public static readonly SharedStatic<int> SharedSub16Count = SharedStatic<int>.GetOrCreate<AnywhenAudioMetronome>();

    [SerializeField] private int bpm = 120;
    public int Bpm => bpm;

    public static event MetronomeTickDelegate OnAudioTick;

    internal static void TriggerTick(MetronomeTickEvent tick)
    {
        OnAudioTick?.Invoke(tick);
    }

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;

    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        return Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, bpm);
    }

    public struct SetBpmMsg
    {
        public int NewBpm;
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
            var availableData = pipe.GetAvailableData(context);
            foreach (var element in availableData)
            {
                if (element.TryGetData(out SetBpmMsg bpmMsg))
                {
                    _bpm = bpmMsg.NewBpm;
                    _sub16Length = (60.0 / _bpm) * 0.25;
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
                EmitTicks(ctx, pipe, _nextTime16);

                _sub16Count++;
                if (_sub16Count >= 16) _sub16Count = 0;
                _nextTime16 += _sub16Length;

                SharedSub16Count.Data = _sub16Count;
            }

            return buffer.frameCount;
        }

        private void EmitTicks(RealtimeContext ctx, ProcessorInstance.Pipe pipe, double time)
        {
            MetronomeTickEvent e = new MetronomeTickEvent
            {
                TickRate = (int)AnywhenMetronome.TickRate.Sub16,
                ScheduledTime = time,
                Count = _sub16Count
            };
            pipe.SendData(ctx, e);

            if (_sub16Count % 2 == 0)
            {
                e.TickRate = (int)AnywhenMetronome.TickRate.Sub8;
                e.Count = _sub16Count / 2;
                pipe.SendData(ctx, e);
            }

            if (_sub16Count % 4 == 0)
            {
                e.TickRate = (int)AnywhenMetronome.TickRate.Sub4;
                e.Count = _sub16Count / 4;
                pipe.SendData(ctx, e);
            }

            if (_sub16Count % 8 == 0)
            {
                e.TickRate = (int)AnywhenMetronome.TickRate.Sub2;
                e.Count = _sub16Count / 8;
                pipe.SendData(ctx, e);
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
                var availableData = pipe.GetAvailableData(context);
                foreach (var element in availableData)
                {
                    if (element.TryGetData(out MetronomeTickEvent tick))
                    {
                        TriggerTick(tick);
                    }
                }
            }

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe, ProcessorInstance.Message message)
            {
                if (message.Is<SetBpmMsg>())
                {
                    pipe.SendData(context, message.Get<AnywhenAudioMetronome.SetBpmMsg>());
                    return ProcessorInstance.Response.Handled;
                }

                return ProcessorInstance.Response.Unhandled;
            }
        }
    }
}

public struct MetronomeTickEvent
{
    public int TickRate;
    public double ScheduledTime;
    public int Count;
}