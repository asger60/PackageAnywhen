using System;
using Anywhen.Synth;
using UnityEngine;


    public class SynthControlLFO : SynthControlBase
    {
        private UInt32 _phase = 0u; // using an integer type automatically ensures limits
        // phase is in [0 ; 2^(32-1)]

        const float PHASE_MAX = 4294967296;
        private float _currentAmp;
        private UInt32 freq__ph_p_smp = 0u;
        private bool _isActive = false;
        private float _fadeInStart, _fadeInEnd;
        public SynthSettingsObjectLFO settings;

        public void UpdateSettings(SynthSettingsObjectLFO settingsObject)
        {
            settings = settingsObject;
            _isActive = true;
            _phase = 0u;
            _currentAmp = 0;
            _fadeInStart = (float)AudioSettings.dspTime;
            _fadeInEnd = (float)AudioSettings.dspTime + settings.fadeInDuration;
            SetFreq(settings.frequency);
        }

        private void Restart()
        {
            if (!_isActive) return;
            _phase = 0u;
            _isActive = true;
            _currentAmp = 0;
            _fadeInStart = (float)AudioSettings.dspTime;
            _fadeInEnd = (float)AudioSettings.dspTime + settings.fadeInDuration;
            SetFreq(settings.frequency);
        }


        public override void DoUpdate()
        {
            if (!_isActive) return;
            _currentAmp = 
                Mathf.InverseLerp(_fadeInStart, _fadeInEnd, (float)AudioSettings.dspTime);
            _phase += freq__ph_p_smp;
        }


        public override void NoteOn()
        {
            if (settings.retrigger)
                Restart();
        }

        private void SetFreq(float freq__hz, int sample_rate = 48000)
        {
            float freq__ppsmp = freq__hz / sample_rate; // periods per sample
            freq__ph_p_smp = (uint)(freq__ppsmp * PHASE_MAX);
        }

        public override float Process(bool unipolar = false)
        {
            if (unipolar)
                return Sin();


            return 1 + Sin() * _currentAmp * (settings.sendAmount / 100f);
        }

        /// Basic oscillators
        /// <returns></returns>
        // Library sine
        // - possibly slow
        private float Sin()
        {
            if (_isActive == false) return 0.0f;
            float ph01 = _phase / PHASE_MAX;
            return Mathf.Sin(ph01 * 6.28318530717959f);
        }
    }
