using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthControlLFO 
    {
        private UInt32 _phase = 0u; // using an integer type automatically ensures limits

        private const float PhaseMax = 4294967296;
        private float _currentAmp;
        private UInt32 _freqPhPSmp = 0u;
        private bool _isActive = false;

        private float _fadeInDuration = 0.01f;

        //private SynthSettingsObjectLFO _settings;
        private float _currentFrequency;
        private float _sendAmount;
        bool _retrigger;

        public void UpdateSettings(AudioLFOSettings newSettings)
        {
            _currentFrequency = newSettings.frequency;
            SetFreq(newSettings.frequency);
            _isActive = true;
            _currentAmp = newSettings.amplitude;
            _sendAmount = 100;
        }
    

        private void Restart()
        {
            if (!_isActive) return;
            _phase = 0u;
            _isActive = true;
            SetFreq(_currentFrequency);
        }


        public  void DoUpdate()
        {
            _phase += _freqPhPSmp;
        }


        public  void NoteOn()
        {
            if (_retrigger)
                Restart();
        }

        private void SetFreq(float freqHz, int sampleRate = 48000)
        {
            float rate = AnywhenRuntime.SampleRate;
            if (rate <= 0) rate = sampleRate;
            if (rate <= 0) rate = 48000;
        
            float freqPpsmp = freqHz / rate; // periods per sample
        
            if (float.IsNaN(freqPpsmp) || float.IsInfinity(freqPpsmp)) freqPpsmp = 0;
        
            _freqPhPSmp = (uint)(freqPpsmp * PhaseMax);
        }

        public  float Process(bool unipolar = false)
        {
            if (unipolar)
                return Sin();

            return 1 + Sin() * _currentAmp * (_sendAmount / 100f);
        }

        /// Basic oscillators
        /// <returns></returns>
        // Library sine
        // - possibly slow
        private float Sin()
        {
            if (!_isActive) return 0.0f;
            float ph01 = _phase / PhaseMax;
            return Mathf.Sin(ph01 * 6.28318530717959f);
        }
    }
}