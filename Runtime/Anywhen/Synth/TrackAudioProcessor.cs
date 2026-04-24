using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct TrackAudioProcessor
    {
        private AudioProcessorSettingsObject.Unmanaged _settings;
        private AudioProcessorLowPass _lowPass;
        private AudioProcessorSaturator _saturator;
        AudioProcessorBandPass _bandPass;
        // Add other filter states here as they are ported to structs

        public TrackAudioProcessor(int sampleRate, AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings;
            _lowPass = default;
            _saturator = default;
            _bandPass = default;

            switch (_settings.filterType)
            {
                // Initialize based on type
                case AudioProcessorSettingsObject.FilterTypes.LowPassFilter:
                    _lowPass = new AudioProcessorLowPass(sampleRate);
                    _lowPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.SaturatorFilter:
                    _saturator = new AudioProcessorSaturator(sampleRate);
                    _saturator.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.BandPassFilter:
                    _bandPass = new AudioProcessorBandPass(sampleRate);
                    _bandPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.FormantFilter:
                    break;
                case AudioProcessorSettingsObject.FilterTypes.LadderFilter:
                    break;
                case AudioProcessorSettingsObject.FilterTypes.BitcrushFilter:
                    break;
                case AudioProcessorSettingsObject.FilterTypes.DelayFilter:
                    break;
                case AudioProcessorSettingsObject.FilterTypes.ChorusFilter:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

    public interface IAudioProcessor
    {
        public void DoUpdate();

        public float Process(float sample);

        public void SetGate(bool gate);

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings);
    }
}