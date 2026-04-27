using PlasticPipe.Server;
using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorChorus : IAudioProcessor, System.IDisposable
    {
        private float _rate;
        private float _depth;
        private float _delay;
        private float _feedback;
        private float _wet;

        private NativeArray<float> _delayBuffer0;
        private NativeArray<float> _delayBuffer1;
        private int _writePos0;
        private int _writePos1;
        private float _lfoPhase0;
        private float _lfoPhase1;
        private int _sampleRate;
        private int _channelCounter;
        private float _filterMod;
        private bool _initialized;
        private AudioProcessorSettingsObject.ChorusSettings.Unmanaged _settings;

        public AudioProcessorChorus(int sampleRate)
        {
            _rate = 0;
            _depth = 0;
            _delay = 0;
            _feedback = 0;
            _wet = 0;
            _sampleRate = sampleRate;
            _channelCounter = 0;
            _filterMod = 1;
            _initialized = false;
            _writePos0 = 0;
            _writePos1 = 0;
            _lfoPhase0 = 0f;
            _lfoPhase1 = 0.25f; // Offset for stereo width
            _settings = new AudioProcessorSettingsObject.ChorusSettings().ToUnmanaged();
            _delayBuffer0 = default;
            _delayBuffer1 = default;
        }

        void UpdateSettings()
        {
            _rate = _settings.rate;
            _depth = _settings.depth;
            _delay = _settings.delay;
            _feedback = _settings.feedback;
            _wet = _settings.wet;

            if (_sampleRate == 0)
            {
                _sampleRate = AudioSettings.outputSampleRate;
                if (_sampleRate <= 0) _sampleRate = 44100;
            }

            if (!_initialized)
            {
                // Buffer for up to 100ms delay, which is plenty for chorus
                int bufferSize = (int)(_sampleRate * 0.1f) + 2;
                _delayBuffer0 = new NativeArray<float>(bufferSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                _delayBuffer1 = new NativeArray<float>(bufferSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                _initialized = true;
            }

            _filterMod = 1;
        }

        public void DoUpdate() { }

        public void SetGate(bool gate) { }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings.chorusSettings;
            UpdateSettings();
        }

        public float Process(float sample, AnywhenAudioGenrator.Processor.Track track)
        {
            UpdateSettings();

            if (!_initialized) return sample;

            int channel = _channelCounter % 2;
            _channelCounter++;

            ref NativeArray<float> buffer = ref (channel == 0 ? ref _delayBuffer0 : ref _delayBuffer1);
            ref int writePos = ref (channel == 0 ? ref _writePos0 : ref _writePos1);
            ref float lfoPhase = ref (channel == 0 ? ref _lfoPhase0 : ref _lfoPhase1);

            // LFO for delay modulation (Sine wave)
            float lfo = Mathf.Sin(lfoPhase * 2f * Mathf.PI);

            // _rate is 0-1, mapped to 0.1Hz - 5Hz
            float mappedRate = Mathf.Lerp(0.1f, 5.0f, _rate);
            lfoPhase += mappedRate / _sampleRate;
            if (lfoPhase > 1f) lfoPhase -= 1f;

            int bufLen = buffer.Length;

            // Base delay 5ms to 50ms, modulation depth up to 5ms
            float baseDelaySamples = Mathf.Lerp(0.005f, 0.050f, _delay) * _sampleRate;
            float modDepthSamples = _depth * _filterMod * 0.005f * _sampleRate;
            float currentDelaySamples = baseDelaySamples + (lfo * modDepthSamples);

            // Calculate read position
            float readPos = writePos - currentDelaySamples;
            while (readPos < 0) readPos += bufLen;
            while (readPos >= bufLen) readPos -= bufLen;

            // Linear interpolation
            int idx1 = (int)readPos % bufLen;
            int idx2 = (idx1 + 1) % bufLen;
            float frac = readPos - (int)readPos;
            float delayedSample = Mathf.Lerp(buffer[idx1], buffer[idx2], frac);

            // Write to buffer with feedback (cap feedback slightly)
            buffer[writePos] = sample + (delayedSample * _feedback * 0.95f);

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