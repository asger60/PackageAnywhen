using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorChorus : IAudioProcessor
    {
        private float _rate;
        private float _depth;
        private float _delay;
        private float _feedback;
        private float _wet;

        private float[][] _delayBuffers;
        private int[] _writePositions;
        private float[] _lfoPhases;
        private int _sampleRate;
        private int _channelCounter;
        private float _filterMod;
        private AudioProcessorSettingsObject.ChorusSettings _settings;

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
            _settings = new AudioProcessorSettingsObject.ChorusSettings();
            _delayBuffers = null;
            _writePositions = null;
            _lfoPhases = null;
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

            // Buffer for up to 100ms delay, which is plenty for chorus
            int bufferSize = (int)(_sampleRate * 0.1f) + 2;
            if (_delayBuffers == null)
            {
                _delayBuffers = new float[2][];
                _delayBuffers[0] = new float[bufferSize];
                _delayBuffers[1] = new float[bufferSize];
                _writePositions = new int[2];
                _lfoPhases = new float[2];
                // Offset second channel phase for stereo width
                _lfoPhases[1] = 0.25f;
            }

            _filterMod = 1;
            //foreach (var mod in ModRoutings)
            //{
            //    _filterMod = mod.Process(_filterMod);
            //}
        }


        public void DoUpdate()
        {
        }

        public void SetGate(bool gate)
        {
        }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings.chorusSettings;
            UpdateSettings();
        }

        public float Process(float sample)
        {
            UpdateSettings();

            int channel = _channelCounter % 2;
            _channelCounter++;

            if (_delayBuffers == null) return sample;

            float[] buffer = _delayBuffers[channel];
            int writePos = _writePositions[channel];

            // LFO for delay modulation (Sine wave)
            float lfo = Mathf.Sin(_lfoPhases[channel] * 2f * Mathf.PI);

            // Advance LFO phase
            // _rate is 0-1, let's map it to 0.1Hz - 5Hz
            float mappedRate = Mathf.Lerp(0.1f, 5.0f, _rate);
            _lfoPhases[channel] += mappedRate / _sampleRate;
            if (_lfoPhases[channel] > 1f) _lfoPhases[channel] -= 1f;

            // Calculate delay in samples
            // Base delay 5ms to 50ms
            float baseDelaySamples = Mathf.Lerp(0.005f, 0.050f, _delay) * _sampleRate;
            // Modulation depth up to 5ms
            float modDepthSamples = _depth * _filterMod * 0.005f * _sampleRate;

            float currentDelaySamples = baseDelaySamples + (lfo * modDepthSamples);

            // Calculate read position
            float readPos = writePos - currentDelaySamples;
            while (readPos < 0) readPos += buffer.Length;
            while (readPos >= buffer.Length) readPos -= buffer.Length;

            // Linear interpolation
            int idx1 = (int)readPos;
            int idx2 = (idx1 + 1) % buffer.Length;
            float frac = readPos - idx1;
            float delayedSample = Mathf.Lerp(buffer[idx1], buffer[idx2], frac);

            // Write to buffer with feedback
            buffer[writePos] = sample + (delayedSample * _feedback * 0.95f); // Cap feedback slightly

            // Advance write position
            _writePositions[channel] = (writePos + 1) % buffer.Length;

            // Mix dry and wet
            return (delayedSample * _wet) + (sample * (1f - _wet));
        }
    }
}