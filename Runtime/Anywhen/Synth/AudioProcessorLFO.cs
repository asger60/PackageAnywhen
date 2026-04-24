using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorLFO : IAudioProcessor
    {
        private float _phase; // using an integer type automatically ensures limits

        private const float PhaseMax = 4294967296;
        private UInt32 _freqPhPSmp;
        private bool _isActive;

        //private SynthSettingsObjectLFO _settings;
        private float _currentFrequency;
        bool _retrigger;
        AudioProcessorSettingsObject.LFOSettings _settings;
        private int _sampleRate;

        public AudioProcessorLFO(int sampleRate)
        {
            _sampleRate = sampleRate;
            _currentFrequency = 100;
            _retrigger = false;
            _freqPhPSmp = 0u;
            _phase = 0u;
            _isActive = false;
            _settings = new AudioProcessorSettingsObject.LFOSettings
            {
                frequency = 10,
                amplitude = 1,
                unipolar = false
            };
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


        public void DoUpdate()
        {
        }

        public float Process(float current, AnywhenAudioGenrator.Processor.Track track)
        {
            return Mathf.Sin(current);
            _phase = current;
            _phase += _freqPhPSmp;

            if (_settings.unipolar)
                return 1 + (Sin() * 0.5f);

            return Sin();
        }

        public void SetGate(bool gate)
        {
        }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings.lfoSettings;
            UpdateSettings();
        }


        public void NoteOn()
        {
            if (_retrigger)
            {
                Restart();
            }
        }

        private void SetFreq(float freqHz)
        {
            float freqPpsmp = freqHz / _sampleRate; // periods per sample

            if (float.IsNaN(freqPpsmp) || float.IsInfinity(freqPpsmp)) freqPpsmp = 0;

            _freqPhPSmp = (uint)(freqPpsmp * PhaseMax);
        }

        private float Sin()
        {
            float ph01 = _phase / PhaseMax;
            return Mathf.Sin(ph01 * 6.28318530717959f);
        }
    }
}