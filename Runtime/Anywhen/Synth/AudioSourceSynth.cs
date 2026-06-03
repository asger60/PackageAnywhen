using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioSourceSynth
    {
        private int _sampleRate;
        private AudioSourceSettings.SynthSourceSettings.Unmanaged _settings;
        private uint _phase;
        private uint _phaseIncrement;
        private float _volume;

        private const uint PhaseMax = uint.MaxValue;

        public AudioSourceSynth(int sampleRate) : this()
        {
            _sampleRate = sampleRate > 0 ? sampleRate : 44100;
            _phase = 0;
            _phaseIncrement = 0;
            _volume = 1f;
        }

        public void QueueNote(int noteIndex)
        {
            noteIndex = AnywhenAudioMetronome.Processor.GetScaledNote(noteIndex);
            noteIndex += 30 + _settings.NoteOffset;
            double freqHz = Midi2Freq(noteIndex) * Mathf.Pow(2f, _settings.Detune / 12f);
            double freqPpsmp = freqHz / _sampleRate;
            _phaseIncrement = (uint)(freqPpsmp * PhaseMax);
            _phase = 0;
            _volume = _settings.SourceVolume;
        }

        private static float Midi2Freq(int note)
        {
            return 440 * Mathf.Pow(2, (note - 49.5f) / 12f);
        }

        public void SetSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings.synthSourceSettings;
            _volume = _settings.SourceVolume;
        }

        public void Process(NativeArray<float> pitchMultiplier, NativeArray<float> channelBuffer)
        {
            for (int frame = 0; frame < channelBuffer.Length; frame++)
            {
                float output = 0;
                float ph01 = (float)_phase / PhaseMax;
                float dt = (_phaseIncrement * pitchMultiplier[frame]) / PhaseMax;

                switch (_settings.SynthType)
                {
                    case AudioSourceSettings.SynthSourceSettings.SynthType.Sine:
                        output = Mathf.Sin(ph01 * Mathf.PI * 2f);
                        break;
                    case AudioSourceSettings.SynthSourceSettings.SynthType.Saw:
                        output = 2.0f * ph01 - 1.0f;
                        output -= PolyBlep(ph01, dt);
                        break;
                    case AudioSourceSettings.SynthSourceSettings.SynthType.Square:
                        output = ph01 < 0.5f ? 1.0f : -1.0f;
                        output += PolyBlep(ph01, dt);
                        output -= PolyBlep((ph01 + 0.5f) % 1.0f, dt);
                        break;
                }

                _phase += (uint)(_phaseIncrement * pitchMultiplier[frame]);

                channelBuffer[frame] += output * _volume * 0.5f; // -6dB offset to match sample volume
            }
        }

        private float PolyBlep(float t, float dt)
        {
            if (t < dt)
            {
                t /= dt;
                return t + t - t * t - 1.0f;
            }

            if (t > 1.0f - dt)
            {
                t = (t - 1.0f) / dt;
                return t * t + t + t + 1.0f;
            }

            return 0.0f;
        }
        

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}