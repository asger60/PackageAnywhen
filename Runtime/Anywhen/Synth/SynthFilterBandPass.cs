using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthFilterBandPass : SynthFilterBase
    {
        public SynthFilterBandPass()
        {
            //_sampleRate = sampleRate;
            _q = 5;
        }

        // // DSP variables
        private float _vF, _vD, _vZ1, _vZ2, _vZ3;
        private float _filterFrequency;
        private float _q; // 1-10
        private float _frequencyMod = 1;

        public void SetFrequency(float freq)
        {
            _filterFrequency = freq * _frequencyMod;
        }

        public void SetQ(float q)
        {
            _q = q;
        }

        public override void SetSettings(SynthSettingsObjectFilter newSettings)
        {
            Init(AnywhenRuntime.SampleRate);
            _q = 5;
            Settings = newSettings;
        }


        public void Init(int sampleRate)
        {
            _frequencyMod = 1;
        }

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            Settings = settingsObjectFilter;
            SetFrequency(settingsObjectFilter.bandPassSettings.frequency);
            _q = settingsObjectFilter.bandPassSettings.bandWidth;
        }

        public override void HandleModifiers(float mod1)
        {
            _frequencyMod = mod1;
        }

        

        public override float Process(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            float sampleRate = AnywhenRuntime.SampleRate;
            if (sampleRate <= 0) sampleRate = 44100;

            float filterFreq = _filterFrequency * _frequencyMod;
            var f = 2f / 1.85f * Mathf.Sin(Mathf.PI * filterFreq / sampleRate);
            
            // Guard against NaN/Inf in coefficients
            if (float.IsNaN(f) || float.IsInfinity(f)) f = 0.1f;

            _vD = 1f / Mathf.Max(_q, 0.01f);
            _vF = (1.85f - 0.75f * _vD * f) * f;
            

            _vZ1 = 0.5f * sample;
            _vZ3 = this._vZ2 * _vF + this._vZ3;
            _vZ2 = (_vZ1 + this._vZ1 - _vZ3 - this._vZ2 * _vD) * _vF + this._vZ2;
            
            // Guard against state explosion
            if (float.IsNaN(_vZ2) || float.IsInfinity(_vZ2))
            {
                _vZ1 = _vZ2 = _vZ3 = 0;
                return 0;
            }

            return _vZ2;
        }
    }
}