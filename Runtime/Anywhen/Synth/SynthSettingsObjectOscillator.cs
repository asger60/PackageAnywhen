

    public class SynthSettingsObjectOscillator : SynthSettingsObjectBase
    {
        public int tuning;
        public float amplitude;

        public enum OscillatorType
        {
            Simple,
            WaveTable,
            Noise
        }

        public enum SimpleOscillatorTypes
        {
            Sine,
            Saw,
            Square,
        }

        public enum WaveTableOscillatorTypes
        {
            Sine8Bit,
            Saw8Bit,
            Square8Bit
        }

        public enum NoiseTypes
        {
            White,
            Brown
        }

        public SimpleOscillatorTypes simpleOscillatorType;
        public NoiseTypes noiseType;


        public WaveTableOscillatorTypes waveTableOscillatorType;
        public OscillatorType oscillatorType;

        public float glideTime = 8000f;
        public bool glide;

        public struct Unmanaged
        {
            public int tuning;
            public float amplitude;
            public SimpleOscillatorTypes simpleOscillatorType;
            public NoiseTypes noiseType;
            public WaveTableOscillatorTypes waveTableOscillatorType;
            public OscillatorType oscillatorType;
            public float glideTime;
            public bool glide;
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                tuning = tuning,
                amplitude = amplitude,
                simpleOscillatorType = simpleOscillatorType,
                noiseType = noiseType,
                waveTableOscillatorType = waveTableOscillatorType,
                oscillatorType = oscillatorType,
                glideTime = glideTime,
                glide = glide
            };
        }

        public void Init()
        {
            amplitude = 1;
            glideTime = 8000f;
        }
    }
