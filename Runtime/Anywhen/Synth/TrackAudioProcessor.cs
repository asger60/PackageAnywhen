using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct TrackAudioProcessor
    {
        private AudioProcessorSettingsObject.Unmanaged _settings;
        private AudioProcessorLowPass _lowPass;
        private AudioProcessorSaturator _saturator;
        private AudioProcessorBandPass _bandPass;
        private AudioProcessorBitcrush _bitcrush;
        private AudioProcessorLadder _ladder;
        private AudioProcessorChorus _chorus;
        private AudioProcessorDelay _delay;

        public TrackAudioProcessor(int sampleRate, AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings;
            _lowPass = default;
            _saturator = default;
            _bandPass = default;
            _bitcrush = default;
            _ladder = default;
            _chorus = default;
            _delay = default;

            switch (_settings.filterType)
            {
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
                    _ladder = new AudioProcessorLadder(sampleRate);
                    _ladder.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.BitcrushFilter:
                    _bitcrush = new AudioProcessorBitcrush(sampleRate);
                    _bitcrush.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.DelayFilter:
                    _delay = new AudioProcessorDelay(sampleRate);
                    _delay.SetSettings(_settings);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.ChorusFilter:
                    _chorus = new AudioProcessorChorus(sampleRate);
                    _chorus.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float Process(float sample)
        {
            return _settings.filterType switch
            {
                AudioProcessorSettingsObject.FilterTypes.LowPassFilter => _lowPass.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.SaturatorFilter => _saturator.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.BandPassFilter => _bandPass.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.BitcrushFilter => _bitcrush.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.LadderFilter => _ladder.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.ChorusFilter => _chorus.Process(sample),
                AudioProcessorSettingsObject.FilterTypes.DelayFilter => _delay.Process(sample),

                _ => sample
            };
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