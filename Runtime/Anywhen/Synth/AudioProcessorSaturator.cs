using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorSaturator : IAudioProcessor
    {
        private float _drive;
        private float _wet;
        private float _filterMod;
        private readonly int _sampleRate;
        AudioProcessorSettings.Unmanaged _settings;


        public AudioProcessorSaturator(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
            _filterMod = 1;
        }

        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings;
        }

        void UpdateSettings()
        {
            _drive = _settings.saturatorSettings.drive;
            _wet = _settings.saturatorSettings.wet;
        }


        public void DoUpdate()
        {
        }

        public void Process(NativeArray<float> buffer, AnysongTrack anysongTrack)
        {
            UpdateSettings();
            for (int frame = 0; frame < buffer.Length; frame++)
            {
                float sample = buffer[frame];
                // Simple soft clipping saturation using tanh-like shaping
                // output = tanh(input * drive)

                float drivenSample = sample * _drive * (_filterMod);

                // Fast approximation of tanh or similar soft clipping
                // Using a simple soft-clipper: x / (1 + abs(x))
                float saturatedSample = drivenSample / (1f + Mathf.Abs(drivenSample));

                // Mix dry and wet
                buffer[frame] = (saturatedSample * _wet) + (sample * (1f - _wet));
            }
        }

        public void SetGate(bool gate)
        {
        }


        public void Dispose()
        {
        }
    }
}