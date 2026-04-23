using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorSaturator : IAudioProcessor
    {
        private float _drive;
        private float _wet;
        private float _filterMod;
        private readonly int _sampleRate;
        AudioProcessorSettingsObject.Unmanaged _settings;

        public void SetExpression(float data)
        {
        }

        public AudioProcessorSaturator(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
            _filterMod = 1;
            //_v = 1.0f / (2.0f * V_t);
            //_frequencyMod = 1;
            //_oversampling = 1;
        }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings;
        }

        void UpdateSettings()
        {
            _drive = _settings.saturatorSettings.drive;
            _wet = _settings.saturatorSettings.wet;
        }

        public void HandleModifiers(float mod1)
        {
        }


        public void DoUpdate()
        {
        }

        public float Process(float sample)
        {
            UpdateSettings();
            // Simple soft clipping saturation using tanh-like shaping
            // output = tanh(input * drive)

            float drivenSample = sample * _drive * (_filterMod);

            // Fast approximation of tanh or similar soft clipping
            // Using a simple soft-clipper: x / (1 + abs(x))
            float saturatedSample = drivenSample / (1f + Mathf.Abs(drivenSample));

            // Mix dry and wet
            return (saturatedSample * _wet) + (sample * (1f - _wet));
        }

        public void SetGate(bool gate)
        {
        }
    }
}