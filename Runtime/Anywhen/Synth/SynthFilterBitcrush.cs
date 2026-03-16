using Anywhen.Synth.Filter;

namespace Anywhen.Synth
{
    public class SynthFilterBitcrush : SynthFilterBase
    {
        private float _bitDepth;
        private int _downsampling;
        private float[] _lastSamples = new float[2];
        private int _downsamplingCounter;

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            Settings = settingsObjectFilter;
            _bitDepth = settingsObjectFilter.bitcrushSettings.bitDepth;
            _downsampling = settingsObjectFilter.bitcrushSettings.downsampling;
        }


        public override void HandleModifiers(float mod1)
        {
        }

        public override void SetSettings(SynthSettingsObjectFilter newSettings)
        {
            Settings = newSettings;
            SetParameters(newSettings);
        }


        public override float Process(float sample)
        {
            SetSettings(Settings);
            // Bitcrush / Quantization
            if (_bitDepth < 24f)
            {
                float levels = UnityEngine.Mathf.Pow(2, _bitDepth);
                sample = UnityEngine.Mathf.Round(sample * levels) / levels;
            }

            // Downsampling
            if (_downsampling > 1)
            {
                int channel = _downsamplingCounter % 2;
                int step = _downsamplingCounter / 2;

                if (step % _downsampling == 0)
                {
                    _lastSamples[channel] = sample;
                }

                _downsamplingCounter++;
                return _lastSamples[channel];
            }

            return sample;
        }
    }
}