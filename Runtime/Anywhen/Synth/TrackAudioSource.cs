using System;

namespace Anywhen.Synth
{
    public struct TrackAudioSource
    {
        private AudioSourceSettings.Unmanaged _settings;

        private AudioSourceSample _sampleSource;
        private AudioSourceSynth _synthSource;
        private AudioSourceNoise _noiseSource;


        public TrackAudioSource(int sampleRate, AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings;
            _sampleSource = default;
            _synthSource = default;
            _noiseSource = default;
            
            switch (_settings.audioSourceType)
            {
                case AudioSourceSettings.AudioSourceTypes.Sample:
                    _sampleSource = new AudioSourceSample(sampleRate);
                    _sampleSource.SetSettings(_settings);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Synth:
                    _synthSource = new AudioSourceSynth(sampleRate);
                    _synthSource.SetSettings(_settings);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Noise:
                    _noiseSource = new AudioSourceNoise(sampleRate);
                    _noiseSource.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public float Process(float sample, float pitchMultiplier)
        {
            return _settings.audioSourceType switch
            {
                AudioSourceSettings.AudioSourceTypes.Sample => _sampleSource.Process(sample, pitchMultiplier),
                AudioSourceSettings.AudioSourceTypes.Synth => _synthSource.Process(sample, pitchMultiplier),
                AudioSourceSettings.AudioSourceTypes.Noise => _noiseSource.Process(sample, pitchMultiplier),
                _ => sample
            };
        }



        public bool IsSamplePlaying => _sampleSource.IsPlaying;

        public void UpdateSettings(AudioSourceSettings.Unmanaged settings)
        {
            if(_settings.Equals(settings)) return;
            _settings = settings;
            switch (_settings.audioSourceType)
            {
                case AudioSourceSettings.AudioSourceTypes.Sample:
                    _sampleSource.SetSettings(_settings);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Synth:
                    _synthSource.SetSettings(_settings);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Noise:
                    _noiseSource.SetSettings(_settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void QueueNote(int noteNoteIndex)
        {
            switch (_settings.audioSourceType)
            {
                case AudioSourceSettings.AudioSourceTypes.Sample:
                    _sampleSource.QueueNote(noteNoteIndex);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Synth:
                    _synthSource.QueueNote(noteNoteIndex);
                    break;
                case AudioSourceSettings.AudioSourceTypes.Noise:
                    _noiseSource.QueueNote(noteNoteIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface IAudioSource
    {

        public float Process(float sample, float pitchMultiplier);

        public void QueueNote(int noteIndex);

        public void SetSettings(AudioSourceSettings.Unmanaged settings);
    }
}