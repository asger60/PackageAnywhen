using System;

namespace Anywhen.Synth
{
    public struct TrackAudioProcessor
    {
        private AudioProcessorSettings.Unmanaged _settings;

        private AudioProcessorLowPass _lowPass;
        private AudioProcessorSaturator _saturator;
        private AudioProcessorBandPass _bandPass;
        private AudioProcessorBitcrush _bitcrush;
        private AudioProcessorLadder _ladder;
        private AudioProcessorChorus _chorus;
        private AudioProcessorDelay _delay;
        private AudioProcessorReverb _reverb;

        public TrackAudioProcessor(int sampleRate, AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings;
            _lowPass = default;
            _saturator = default;
            _bandPass = default;
            _bitcrush = default;
            _ladder = default;
            _chorus = default;
            _delay = default;
            _reverb = default;
            
            switch (_settings.filterType)
            {
                case AudioProcessorSettings.FilterTypes.LowPassFilter:
                    _lowPass = new AudioProcessorLowPass(sampleRate);
                    _lowPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.SaturatorFilter:
                    _saturator = new AudioProcessorSaturator(sampleRate);
                    _saturator.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.BandPassFilter:
                    _bandPass = new AudioProcessorBandPass(sampleRate);
                    _bandPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.FormantFilter:
                    break;
                case AudioProcessorSettings.FilterTypes.LadderFilter:
                    _ladder = new AudioProcessorLadder(sampleRate);
                    _ladder.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.BitcrushFilter:
                    _bitcrush = new AudioProcessorBitcrush(sampleRate);
                    _bitcrush.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.DelayFilter:
                    _delay = new AudioProcessorDelay(sampleRate);
                    _delay.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.ChorusFilter:
                    _chorus = new AudioProcessorChorus(sampleRate);
                    _chorus.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.ReverbFilter:
                    _reverb = new AudioProcessorReverb(sampleRate);
                    _reverb.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float Process(float sample, AnywhenAudioGenerator.Processor.Track track)
        {
            return _settings.filterType switch
            {
                AudioProcessorSettings.FilterTypes.LowPassFilter => _lowPass.Process(sample, track),
                AudioProcessorSettings.FilterTypes.SaturatorFilter => _saturator.Process(sample, track),
                AudioProcessorSettings.FilterTypes.BandPassFilter => _bandPass.Process(sample, track),
                AudioProcessorSettings.FilterTypes.BitcrushFilter => _bitcrush.Process(sample, track),
                AudioProcessorSettings.FilterTypes.LadderFilter => _ladder.Process(sample, track),
                AudioProcessorSettings.FilterTypes.ChorusFilter => _chorus.Process(sample, track),
                AudioProcessorSettings.FilterTypes.DelayFilter => _delay.Process(sample, track),
                AudioProcessorSettings.FilterTypes.ReverbFilter => _reverb.Process(sample, track),

                _ => sample
            };
        }

        public void UpdateSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings;
            switch (_settings.filterType)
            {
                case AudioProcessorSettings.FilterTypes.LowPassFilter:
                    _lowPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.SaturatorFilter:
                    _saturator.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.BandPassFilter:
                    _bandPass.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.FormantFilter:
                    break;
                case AudioProcessorSettings.FilterTypes.LadderFilter:
                    _ladder.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.BitcrushFilter:
                    _bitcrush.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.DelayFilter:
                    _delay.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.ChorusFilter:
                    _chorus.SetSettings(_settings);
                    break;
                case AudioProcessorSettings.FilterTypes.ReverbFilter:
                    _reverb.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface IAudioProcessor
    {
        public void DoUpdate();

        public float Process(float sample, AnywhenAudioGenerator.Processor.Track track);

        public void SetGate(bool gate);

        public void SetSettings(AudioProcessorSettings.Unmanaged settings);
    }
}