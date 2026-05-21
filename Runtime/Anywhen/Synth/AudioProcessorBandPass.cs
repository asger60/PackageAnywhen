using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorBandPass : IAudioProcessor, System.IDisposable
    {
        //_sampleRate = sampleRate;

        // // DSP variables
        private float _vF, _vD, _vZ1, _vZ2, _vZ3;
        private float _filterFrequency;
        private float _q ; // 1-10
        private float _frequencyMod ;
        private readonly int _sampleRate;
        AudioProcessorSettings.BandPassSettings.Unmanaged _settings;
        public AudioProcessorBandPass(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
            _q = 5; // 1-10
            _frequencyMod = 1;
        }

        public void SetFrequency(float freq)
        {
            _filterFrequency = freq * _frequencyMod;
        }

        public void SetQ(float q)
        {
            _q = q;
        }

        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings.bandPassSettings;
        }


        public void Init()
        {
            _frequencyMod = 1;
        }



          void UpdateSettings()
        {
            SetFrequency(_settings.frequency);
            _q = _settings.q;
            _frequencyMod = 1;
            //foreach (var mod in ModRoutings)
            //{
            //    _frequencyMod = mod.Process(_frequencyMod);
            //}
        }


        public void DoUpdate()
        {
        }

        public float Process(float sample, AnysongTrack anysongTrack)
        {
            UpdateSettings();

            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            float sampleRate = 0;
            if (sampleRate <= 0) sampleRate = 44100;

            float filterFreq = _filterFrequency * _frequencyMod;
            var f = 2f / 1.85f * Mathf.Sin(Mathf.PI * filterFreq / sampleRate);

            // Guard against NaN/Inf in coefficients
            if (float.IsNaN(f) || float.IsInfinity(f)) f = 0.1f;

            _vD = 1f / Mathf.Max(_q * 0.5f, 0.01f);
            _vF = (1.85f - 0.75f * _vD * f) * f;
            _vF = Mathf.Clamp(_vF, 0, 1.99f);

            _vZ1 = 0.5f * sample;
            _vZ3 = _vZ2 * _vF + _vZ3;
            _vZ2 = (_vZ1 + _vZ1 - _vZ3 - _vZ2 * _vD) * _vF + _vZ2;

            // Guard against state explosion
            if (float.IsNaN(_vZ2) || float.IsInfinity(_vZ2))
            {
                _vZ1 = _vZ2 = _vZ3 = 0;
                return 0;
            }

            return _vZ2;
        }

        public void SetGate(bool gate)
        {
        }

        public void Dispose()
        {
        }
    }
}