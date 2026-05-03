using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorDelay : IAudioProcessor, System.IDisposable
    {
        private float _delayTime;
        private float _feedback;
        private float _wet;

        private NativeArray<float> _delayBuffer0;
        private NativeArray<float> _delayBuffer1;
        private int _writePos0;
        private int _writePos1;
        private int _sampleRate;
        private int _channelCounter;
        private bool _initialized;

        private AudioProcessorSettings.DelaySettings.Unmanaged _settings;

        public AudioProcessorDelay(int sampleRate)
        {
            _delayTime = 0;
            _feedback = 0;
            _wet = 0;
            _sampleRate = sampleRate;
            _channelCounter = 0;
            _initialized = false;
            _writePos0 = 0;
            _writePos1 = 0;
            _settings = new AudioProcessorSettings.DelaySettings().ToUnmanaged();
            _delayBuffer0 = default;
            _delayBuffer1 = default;
        }

        void UpdateSettings()
        {
            _delayTime = _settings.delayTime;
            _feedback = _settings.feedback;
            _wet = _settings.wet;

            if (_sampleRate == 0)
            {
                _sampleRate = AudioSettings.outputSampleRate;
                if (_sampleRate == 0) _sampleRate = 44100;
            }

            if (!_initialized)
            {
                // Max delay of 2 seconds to be safe
                int bufferSize = _sampleRate * 2;
                _delayBuffer0 = new NativeArray<float>(bufferSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                _delayBuffer1 = new NativeArray<float>(bufferSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                _initialized = true;
            }
        }

        public void SetGate(bool gate) { }

        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings.delaySettings;
            UpdateSettings();
        }

        public void DoUpdate() { }

        public float Process(float sample, AnysongTrack anysongTrack)
        {
            UpdateSettings();

            if (!_initialized) return sample;

            int channel = _channelCounter % 2;
            _channelCounter++;

            ref NativeArray<float> buffer = ref (channel == 0 ? ref _delayBuffer0 : ref _delayBuffer1);
            ref int writePos = ref (channel == 0 ? ref _writePos0 : ref _writePos1);

            // Calculate read position
            float delayInSamples = _delayTime * _sampleRate;
            float readPos = writePos - delayInSamples;

            // Wrap read position
            int bufLen = buffer.Length;
            while (readPos < 0) readPos += bufLen;

            // Linear interpolation
            int idx1 = (int)readPos % bufLen;
            int idx2 = (idx1 + 1) % bufLen;
            float frac = readPos - (int)readPos;

            float delayedSample = Mathf.Lerp(buffer[idx1], buffer[idx2], frac);

            // Write to buffer (input + feedback)
            buffer[writePos] = sample + (delayedSample * _feedback);

            // Advance write position
            writePos = (writePos + 1) % bufLen;

            // Mix dry and wet
            return (delayedSample * _wet) + (sample * (1f - _wet));
        }

        public void Dispose()
        {
            if (_delayBuffer0.IsCreated) _delayBuffer0.Dispose();
            if (_delayBuffer1.IsCreated) _delayBuffer1.Dispose();
            _initialized = false;
        }
    }
}