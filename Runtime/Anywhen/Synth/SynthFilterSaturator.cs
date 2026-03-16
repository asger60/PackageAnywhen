using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthFilterSaturator : SynthFilterBase
    {
        private float _drive;
        private float _wet;

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            Settings = settingsObjectFilter;
            _drive = settingsObjectFilter.saturatorSettings.drive;
            _wet = settingsObjectFilter.saturatorSettings.wet;
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
            // Simple soft clipping saturation using tanh-like shaping
            // output = tanh(input * drive)
            
            float drivenSample = sample * _drive;
            
            // Fast approximation of tanh or similar soft clipping
            // Using a simple soft-clipper: x / (1 + abs(x))
            float saturatedSample = drivenSample / (1f + Mathf.Abs(drivenSample));
            
            // Mix dry and wet
            return (saturatedSample * _wet) + (sample * (1f - _wet));
        }
    }
}
