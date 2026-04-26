using Anywhen.Synth.Filter;
using PlasticPipe.Server;

namespace Anywhen.Synth
{
    // A 303-style diode ladder filter.
    // Diode ladders have a specific resonance behavior and a shallower slope (18dB-ish)
    // compared to Moog-style transistor ladders (24dB/oct).
    public struct AudioProcessorLadder : IAudioProcessor
    {
        private float _cutoffMod;

        private float _resolution;
        private int _oversampling;

        // State variables for the diode ladder stages
        private float _s1, _s2, _s3, _s4;
        private float _g;
        private float _h;
        private float _frequencyMod;


        private AudioProcessorSettingsObject.LadderSettings.Unmanaged _settings;
        int _sampleRate;

        public AudioProcessorLadder(int sampleRate)
        {
            _resolution = 0;
            _s1 = _s2 = _s3 = _s4 = 0;
            _g = _h = 0;
            _frequencyMod = 1;
            _cutoffMod = 1;
            _oversampling = 1;
            _settings = new AudioProcessorSettingsObject.LadderSettings().ToUnmanaged();
            _sampleRate = sampleRate;
        }


        public void SetGate(bool gate)
        {
        }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings.ladderSettings;
            UpdateSettings();
        }

        private void UpdateSettings()
        {
            _resolution = _settings.resonance;
            SetCutOff(_settings.cutoffFrequency);
            SetOversampling(_settings.oversampling);
        }


        public void DoUpdate()
        {
        }

        public float Process(float sample, AnywhenAudioGenrator.Processor.Track track)
        {
            if (_settings.cutoffMod is { IsCreated: true, Length: > 0 })
                _frequencyMod = track.GetModSignal(_settings.cutoffMod);
            UpdateSettings();
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            // Improved TPT (Topology Preserving Transform) 303-style diode ladder model.
            // Diode ladder characteristics: stages load each other, feedback is non-linear.

            float k = _resolution * 17.0f; // Resonance range tuning
            k = Clamp(k, 0, 16.5f); // Keep it within stable self-oscillation limit

            for (int j = 0; j < _oversampling; ++j)
            {
                // Calculate feedback based on previous outputs to solve the delay-free loop
                // In a true TPT diode ladder, this is a bit complex due to stage coupling.
                // We use a simplified coupling approximation.

                float G = _g;
                float G2 = G * G;
                float G3 = G2 * G;

                // Simplified estimate of the filter's "instantaneous response" (S)
                // for the delay-free loop resolution: x = (input - k*S) / (1 + k*gamma)
                // gamma is the feedback gain through the stages.
                float S = (G3 * G * _s1 + G3 * _s2 + G2 * _s3 + G * _s4) / (1.0f + G);
                float gamma = G3 * G / (1.0f + G);

                float input = sample;
                float x = (input - k * S) / (1.0f + k * gamma);

                // Stage 1
                float v1 = (FastTanh(x) - _s1) * _h;
                float y1 = v1 + _s1;
                _s1 = y1 + v1;

                // Stage 2
                float v2 = (y1 - _s2) * _h;
                float y2 = v2 + _s2;
                _s2 = y2 + v2;

                // Stage 3
                float v3 = (y2 - _s3) * _h;
                float y3 = v3 + _s3;
                _s3 = y3 + v3;

                // Stage 4
                float v4 = (y3 - _s4) * _h;
                float y4 = v4 + _s4;
                _s4 = y4 + v4;

                sample = y4;

                // Guard against state explosion
                if (float.IsNaN(sample) || float.IsInfinity(sample))
                {
                    _s1 = _s2 = _s3 = _s4 = 0;
                    sample = 0;
                    break;
                }
            }

            return Clamp(sample, -1f, 1f);
        }


        // Fast approximation of Tanh from SynthFilterLowPass
        private static float FastTanh(float x)
        {
            if (x < -3) return -1;
            if (x > 3) return 1;
            float x2 = x * x;
            return x * (27 + x2) / (27 + 9 * x2);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private void SetCutOff(float frequency)
        {
            // Proper frequency mapping for TPT filters
            if (_sampleRate <= 0) _sampleRate = 44100;
            float omega = 2.0f * 3.14159265f * frequency / (_sampleRate * _oversampling) * _cutoffMod * _frequencyMod;

            // Guard against NaN/Inf
            if (float.IsNaN(omega) || float.IsInfinity(omega)) omega = 0.1f;

            // Guard against Tan(PI/2) and instability at high frequencies
            omega = Clamp(omega, 0, 3.1f);

            _g = (float)System.Math.Tan(omega * 0.5f);
            _g = Clamp(_g, 0, 100); // Allow some high-frequency response but avoid Infinity
            _h = _g / (1.0f + _g);
        }

        private void SetOversampling(int iterationCount)
        {
            _oversampling = iterationCount;
            if (_oversampling < 1)
                _oversampling = 1;
        }
    }
}