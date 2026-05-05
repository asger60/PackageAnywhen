
namespace Anywhen.Synth
{
    public struct AudioSourceNoise : IAudioSource
    {
        private AudioSourceSettings.NoiseSourceSettings.Unmanaged _settings;
        private bool _gate;
        private float _volume;

        // Pink noise state (Paul Kellet's refined method)
        private float _b0, _b1, _b2, _b3, _b4, _b5, _b6;

        // Brown noise state
        private float _brownLast;

        // Simple xorshift RNG state for burst-safe noise
        private uint _rngState;

        public AudioSourceNoise(int sampleRate) : this()
        {
            _gate = false;
            _volume = 1f;
            _b0 = _b1 = _b2 = _b3 = _b4 = _b5 = _b6 = 0f;
            _brownLast = 0f;
            _rngState = 12345;
        }

        public void QueueNote(int noteIndex)
        {
            _gate = true;
            _volume = _settings.SourceVolume;
        }

        public void SetSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings.noiseSourceSettings;
            _volume = _settings.SourceVolume;
        }

        public float Process(float sample, float pitchMultiplier = 1)
        {
            if (!_gate) return 0;

            float output = 0;

            switch (_settings.NoiseType)
            {
                case AudioSourceSettings.NoiseSourceSettings.NoiseType.White:
                    output = NextWhite();
                    break;
                case AudioSourceSettings.NoiseSourceSettings.NoiseType.Pink:
                    output = NextPink();
                    break;
                case AudioSourceSettings.NoiseSourceSettings.NoiseType.Brown:
                    output = NextBrown();
                    break;
            }

            return output * _volume * 0.5f; // -6dB offset to match sample volume
        }

        public void SetGate(bool gate)
        {
            _gate = gate;
        }

        /// <summary>
        /// Xorshift32 PRNG returning a float in [-1, 1].
        /// </summary>
        private float NextWhite()
        {
            _rngState ^= _rngState << 13;
            _rngState ^= _rngState >> 17;
            _rngState ^= _rngState << 5;
            return (_rngState / (float)uint.MaxValue) * 2f - 1f;
        }

        /// <summary>
        /// Paul Kellet's refined pink noise filter (−3 dB/octave).
        /// </summary>
        private float NextPink()
        {
            float white = NextWhite();
            _b0 = 0.99886f * _b0 + white * 0.0555179f;
            _b1 = 0.99332f * _b1 + white * 0.0750759f;
            _b2 = 0.96900f * _b2 + white * 0.1538520f;
            _b3 = 0.86650f * _b3 + white * 0.3104856f;
            _b4 = 0.55000f * _b4 + white * 0.5329522f;
            _b5 = -0.7616f * _b5 - white * 0.0168980f;
            float pink = _b0 + _b1 + _b2 + _b3 + _b4 + _b5 + _b6 + white * 0.5362f;
            _b6 = white * 0.115926f;
            return pink * 0.11f;
        }

        /// <summary>
        /// Brown (Brownian / red) noise via leaky integration of white noise (−6 dB/octave).
        /// </summary>
        private float NextBrown()
        {
            float white = NextWhite();
            _brownLast = (_brownLast + 0.02f * white) / 1.02f;
            return _brownLast * 3.5f;
        }
    }
}
