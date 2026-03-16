using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthFilterDelay : SynthFilterBase
    {
        private float _delayTime;
        private float _feedback;
        private float _wet;

        private float[][] _delayBuffers;
        private int[] _writePositions;
        private int _sampleRate;
        private int _channelCounter;

        public override void SetExpression(float data)
        {
        }

        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            Settings = settingsObjectFilter;
            _delayTime = settingsObjectFilter.delaySettings.delayTime;
            _feedback = settingsObjectFilter.delaySettings.feedback;
            _wet = settingsObjectFilter.delaySettings.wet;

            if (_sampleRate == 0)
            {
                _sampleRate = AudioSettings.outputSampleRate;
                if (_sampleRate == 0) _sampleRate = 44100; // Fallback
            }

            // Max delay of 2 seconds to be safe
            int bufferSize = _sampleRate * 2;
            if (_delayBuffers == null)
            {
                _delayBuffers = new float[2][];
                _delayBuffers[0] = new float[bufferSize];
                _delayBuffers[1] = new float[bufferSize];
                _writePositions = new int[2];
            }
        }

        public override void HandleModifiers(float mod1)
        {
        }

        public override void SetSettings(SynthSettingsObjectFilter newSettings)
        {
            Settings = newSettings;
            SetParameters(newSettings);
        }

        public override float Process(float sample)
        {
            SetSettings(Settings);

            int channel = _channelCounter % 2;
            _channelCounter++;

            if (_delayBuffers == null) return sample;

            float[] buffer = _delayBuffers[channel];
            int writePos = _writePositions[channel];

            // Calculate read position
            float delayInSamples = _delayTime * _sampleRate;
            float readPos = writePos - delayInSamples;
            
            // Wrap read position
            while (readPos < 0) readPos += buffer.Length;
            
            // Simple linear interpolation
            int idx1 = (int)readPos;
            int idx2 = (idx1 + 1) % buffer.Length;
            float frac = readPos - idx1;
            
            float delayedSample = Mathf.Lerp(buffer[idx1], buffer[idx2], frac);

            // Write to buffer (input + feedback)
            buffer[writePos] = sample + (delayedSample * _feedback);
            
            // Advance write position
            _writePositions[channel] = (writePos + 1) % buffer.Length;

            // Mix dry and wet
            return (delayedSample * _wet) + (sample * (1f - _wet));
        }
    }
}
