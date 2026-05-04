using System;

namespace Anywhen.Synth
{
    public struct TrackAudioSource
    {
        private AudioSourceSettings.Unmanaged _settings;

        private AudioSourceSample _sampleSource;
        private AudioSourceSynth _synthSource;


        public TrackAudioSource(int sampleRate, AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings;
            _sampleSource = default;
            _synthSource = default;

            
            switch (_settings.audioSourceType)
            {
                

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float Process(float sample)
        {
            return _settings.audioSourceType switch
            {
                AudioSourceSettings.AudioSourceTypes.Sample=> _sampleSource.Process(sample),
                AudioSourceSettings.AudioSourceTypes.Synth => _synthSource.Process(sample),

                _ => sample
            };
        }

        public void UpdateSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings;
            switch (_settings.audioSourceType)
            {
                case AudioSourceSettings.AudioSourceTypes.Sample:
                    _sampleSource.SetSettings(_settings);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Synth:
                    _synthSource.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface IAudioSource
    {
        public void DoUpdate();

        public float Process(float sample);
        

        public void SetSettings(AudioSourceSettings.Unmanaged settings);
    }
}