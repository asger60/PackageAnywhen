using UnityEngine;

namespace Anywhen.Synth
{
    public interface IAudioProcessor
    {
        public void DoUpdate();

        public float Process(float sample);

        public void SetGate(bool gate);
    }

    public struct TrackAudioProcessor
    {
        private AudioProcessorSettingsObject.Unmanaged _settings;
        private AudioProcessorLowPass _lowPass;
        AudioProcessorSaturator _saturator;
        // Add other filter states here as they are ported to structs

        public TrackAudioProcessor(int sampleRate, AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings;
            _lowPass = new AudioProcessorLowPass(sampleRate);
            // Initialize based on type
            if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.LowPassFilter)
            {
                _lowPass.SetCutOff(_settings.lowPassSettings.cutoffFrequency);
                _lowPass.SetResonance(_settings.lowPassSettings.resonance);
                _lowPass.SetOversampling(_settings.lowPassSettings.oversampling);
            }

            _saturator = new AudioProcessorSaturator(sampleRate);

            if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.SaturatorFilter)
            {
                Debug.Log("Saturator");
                _saturator.SetSettings(_settings);
            }
        }

        public float Process(float sample)
        {
            switch (_settings.filterType)
            {
                case AudioProcessorSettingsObject.FilterTypes.LowPassFilter:
                    return _lowPass.Process(sample);
                case AudioProcessorSettingsObject.FilterTypes.SaturatorFilter:
                    return _saturator.Process(sample);

                default:
                    return sample;
            }
        }
    }
}