using Anywhen.Synth.Filter;
using UnityEngine;
using UnitySynth.Runtime.Synth;

namespace Anywhen.Synth
{
    public class SynthFilterBandPass : SynthFilterBase
    {
        public SynthFilterBandPass(float sampleRate)
        {
            //_sampleRate = sampleRate;
            _q = 5;
        }

        float _sampleRate; // Sample rate

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
            Init(AnywhenSynth.SampleRate);
            _q = 5;
            settings = newSettings;
        }


        public void Init(int sampleRate)
        {
            _sampleRate = sampleRate;
            _frequencyMod = 1;
        }

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            settings = settingsObjectFilter;
            SetFrequency(settingsObjectFilter.bandPassSettings.frequency);
            _q = settingsObjectFilter.bandPassSettings.bandWidth;
        }

        public override void HandleModifiers(float mod1)
        {
            _frequencyMod = mod1;
        }

        

        public override float Process(float sample)
        {
            var f = 2f / 1.85f * Mathf.Sin(Mathf.PI * _filterFrequency / _sampleRate);
            _vD = 1f / _q;
            _vF = (1.85f - 0.75f * _vD * f) * f;
            

            _vZ1 = 0.5f * sample;
            _vZ3 = this._vZ2 * _vF + this._vZ3;
            _vZ2 = (_vZ1 + this._vZ1 - _vZ3 - this._vZ2 * _vD) * _vF + this._vZ2;
            
            //sample = _vZ2;
            //this._vZ1 = vZ1;
            //this._vZ2 = vZ2;
            //this._vZ3 = vZ3;

            return _vZ2;
        }
    }
}