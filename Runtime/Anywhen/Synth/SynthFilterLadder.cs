using Anywhen.Synth.Filter;

namespace Anywhen.Synth
{
    // A 303-style diode ladder filter.
    // Diode ladders have a specific resonance behavior and a shallower slope (18dB-ish)
    // compared to Moog-style transistor ladders (24dB/oct).
    public class SynthFilterLadder : SynthFilterBase
    {
        private float _cutoffMod = 1;

        private float _reso;
        private int _oversampling = 1;

        // State variables for the diode ladder stages
        private float s1, s2, s3, s4;
        private float _g;
        private float _h;

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            Settings = settingsObjectFilter;
            _reso = settingsObjectFilter.ladderSettings.resonance;
            SetCutOff(settingsObjectFilter.ladderSettings.cutoffFrequency);
            SetOversampling(settingsObjectFilter.ladderSettings.oversampling);
        }

        public override void HandleModifiers(float mod1)
        {
            _cutoffMod = mod1;
            SetCutOff(Settings.ladderSettings.cutoffFrequency);
        }

        public override void SetSettings(SynthSettingsObjectFilter newSettings)
        {
            _cutoffMod = 1;
            Settings = newSettings;
            SetParameters(newSettings);
        }

        public override float Process(float sample)
        {
            // Improved TPT (Topology Preserving Transform) 303-style diode ladder model.
            // Diode ladder characteristics: stages load each other, feedback is non-linear.
            
            float k = _reso * 17.0f; // Resonance range tuning
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
                float S = (G3 * G * s1 + G3 * s2 + G2 * s3 + G * s4) / (1.0f + G); 
                float gamma = G3 * G / (1.0f + G);

                float input = sample;
                float x = (input - k * S) / (1.0f + k * gamma);

                // Stage 1
                float v1 = (FastTanh(x) - s1) * _h;
                float y1 = v1 + s1;
                s1 = y1 + v1;

                // Stage 2
                float v2 = (y1 - s2) * _h;
                float y2 = v2 + s2;
                s2 = y2 + v2;

                // Stage 3
                float v3 = (y2 - s3) * _h;
                float y3 = v3 + s3;
                s3 = y3 + v3;

                // Stage 4
                float v4 = (y3 - s4) * _h;
                float y4 = v4 + s4;
                s4 = y4 + v4;

                sample = y4;
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
            float omega = 2.0f * 3.14159265f * frequency / (AnywhenRuntime.SampleRate * _oversampling) * _cutoffMod;
            _g = (float)System.Math.Tan(omega * 0.5f);
            _g = Clamp(_g, 0, 1); // Stay within stable range
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