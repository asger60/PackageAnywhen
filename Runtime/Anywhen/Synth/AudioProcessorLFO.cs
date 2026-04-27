using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorLFO : IAudioProcessor
    {
        // FIX 1: uint so overflow wraps naturally at 2^32,
        //         matching the PhaseMax constant design intent
        private uint _phase;

        private const float PhaseMax = 4294967296f; // 2^32
        private uint _freqPhPSmp;
        private bool _isActive;

        private float _currentFrequency;
        bool _retrigger;
        AudioProcessorSettingsObject.LFOSettings.Unmanaged _settings;
        private int _sampleRate;

        public AudioProcessorLFO(int sampleRate)
        {
            _sampleRate = sampleRate;
            _currentFrequency = 0.5f;
            _retrigger = false;
            _freqPhPSmp = 0u;
            _phase = 0u;
            _isActive = false;
            _settings = default;
            UpdateSettings();
        }

        void UpdateSettings()
        {
            _currentFrequency = _settings.frequency;
            SetFreq(_settings.frequency);
            _isActive = true;
        }

        private void Restart()
        {
            if (!_isActive) return;
            _phase = 0u;
            _isActive = true;
            SetFreq(_currentFrequency);
        }

        public void DoUpdate() { }

        public float Process(float current, AnywhenAudioGenrator.Processor.Track track)
        {
            // FIX 2: removed "_phase = current" — that was clobbering the
            //         phase accumulator with the audio input every single sample.
            //         The phase must only ever be incremented.
            _phase += _freqPhPSmp;

            float sin = Sin();

            if (_settings.unipolar)
                return (sin + 1f) * 0.5f;

            return sin * _settings.amplitude;
        }

        public void SetGate(bool gate) { }
        
        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            
            
        }


        public void SetSettings(AudioProcessorSettingsObject.LFOSettings.Unmanaged settings)
        {
            _settings = settings;
            UpdateSettings();
        }

        public void NoteOn()
        {
            if (_retrigger)
                Restart();
        }

        private void SetFreq(float freqHz)
        {
            float freqPpsmp = freqHz / _sampleRate;

            if (float.IsNaN(freqPpsmp) || float.IsInfinity(freqPpsmp))
                freqPpsmp = 0;

            _freqPhPSmp = (uint)(freqPpsmp * PhaseMax);
        }

        private float Sin()
        {
            // uint _phase divided by PhaseMax maps [0 .. 2^32) → [0 .. 1)
            float ph01 = _phase / PhaseMax;
            return Mathf.Sin(ph01 * 2f * Mathf.PI);
        }
    }
}