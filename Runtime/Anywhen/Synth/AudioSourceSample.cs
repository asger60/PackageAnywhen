using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioSourceSample : IAudioSource
    {
        private int _sampleRate;
        AudioSourceSettings.SampleSourceSettings.Unmanaged _settings;

        private AnywhenNoteClip.Unmanaged _clipData;
        private int _sampleCount;
        private double _samplePosition;
        private float _clipPitch;
        private float _trackPitch;
        private bool _isPlaying;
        float _volume;

        public bool IsPlaying => _isPlaying;

        public AudioSourceSample(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
            _trackPitch = 1;
        }

        public void DoUpdate()
        {
        }

        public void QueueNote(int noteIndex)
        {
            var clipSettings = _settings.SampleInstrument.GetNoteClipSettings(noteIndex);
            _clipData = clipSettings.NoteClipUnmanaged;
            _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
            _clipPitch = clipSettings.clipPitch;
            _samplePosition = 0;
            _isPlaying = true;
            _volume = _settings.SourceVolume;
        }

        public void SetSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings.sampleSourceSettings;
            _volume = _settings.SourceVolume;
        }



        public float Process(float sample, float pitchMultiplier)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) return 0;

            int channels = _clipData.channels;
            int frameCount = channels > 0 ? _sampleCount / channels : 0;

            if (_clipData.clipSamples.IsCreated && _samplePosition < frameCount)
            {
                int index = (int)_samplePosition;
                float t = (float)(_samplePosition - index);

                float frameAmplitude = sample;
                bool canInterpolate = index < frameCount - 1;

                for (int c = 0; c < channels; c++)
                {
                    float s0 = _clipData.clipSamples[index * channels + c];
                    if (canInterpolate)
                    {
                        float s1 = _clipData.clipSamples[(index + 1) * channels + c];
                        frameAmplitude += s0 + t * (s1 - s0);
                    }
                    else
                    {
                        frameAmplitude += s0;
                    }
                }

                if (channels > 1)
                    frameAmplitude /= channels;

                _samplePosition += _clipPitch * _trackPitch * pitchMultiplier;
                return frameAmplitude * _volume;
            }

            _isPlaying = false;
            return 0;
        }


        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}