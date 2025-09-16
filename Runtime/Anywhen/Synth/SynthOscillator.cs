using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Synth
{
    public class SynthOscillator
    {
        public UInt32 phase => _phase;
        private UInt32 _phase = 0u; // using an integer type automatically ensures limits
        // phase is in [0 ; 2^(32-1)]

        const float PHASE_MAX = 4294967296;
        private float _amp = 1.0f;
        private UInt32 _freqPhPSmp = 0u;
        private float _pitchModAmount;
        private float _fineTune;


        private int _currentNote;


        private readonly int _waveTableSize = 128;
        private float[] _sine;
        private float[] _sine8Bit;
        private float[] _square8Bit;
        private float[] _saw8Bit;
        private float[] _noiseWhite;

        public SynthSettingsObjectOscillator settings;
        private bool _isActive;
        public bool IsActive => _isActive;

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            _isActive = true;
            switch (settings.oscillatorType)
            {
                case SynthSettingsObjectOscillator.OscillatorType.Simple:
                    _sine = new float[2048];

                    for (int i = 0; i < 2048; ++i)
                    {
                        float angle01 = ((float)i) / 2048;
                        _sine[i] = Mathf.Sin(angle01 * 2 * Mathf.PI);
                    }

                    break;
                case SynthSettingsObjectOscillator.OscillatorType.WaveTable:
                    _sine8Bit = new float[_waveTableSize];
                    _saw8Bit = new float[_waveTableSize];
                    _square8Bit = new float[2];
                    for (int i = 0; i < _waveTableSize; ++i)
                    {
                        float angle01 = ((float)i) / _waveTableSize;
                        _sine8Bit[i] = Mathf.Round(Mathf.Sin(angle01 * 2 * Mathf.PI) * 128) / 128;
                    }

                    for (int i = 0; i < _waveTableSize; ++i)
                    {
                        _saw8Bit[i] = Mathf.Lerp(-1, 1, (float)i / _waveTableSize);
                    }

                    _square8Bit[0] = -1;
                    _square8Bit[1] = 1;
                    break;
                case SynthSettingsObjectOscillator.OscillatorType.Noise:

                    _noiseWhite = new float[16384];
                    for (int i = 0; i < _noiseWhite.Length; ++i)
                    {
                        _noiseWhite[i] = Random.Range(-1f, 1f);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ResetPhase()
        {
            _phase = 0u;
        }

        public void DoUpdate()
        {
            _phase += _freqPhPSmp;
        }


        public float Process()
        {
            if (!_isActive) return 0;
            switch (settings.oscillatorType)
            {
                case SynthSettingsObjectOscillator.OscillatorType.Simple:
                    switch (settings.simpleOscillatorType)
                    {
                        case SynthSettingsObjectOscillator.SimpleOscillatorTypes.Sine:
                            return Sin() * settings.amplitude;
                        case SynthSettingsObjectOscillator.SimpleOscillatorTypes.Saw:
                            return sawPolyBLEP() * settings.amplitude;
                        case SynthSettingsObjectOscillator.SimpleOscillatorTypes.Square:
                            return squarePolyBLEP(0.5f) * settings.amplitude;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case SynthSettingsObjectOscillator.OscillatorType.WaveTable:
                    switch (settings.waveTableOscillatorType)
                    {
                        case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Saw8Bit:
                            return WaveTable() * settings.amplitude;
                        case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Sine8Bit:
                            return WaveTable() * settings.amplitude;
                        case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Square8Bit:
                            return WaveTable() * settings.amplitude;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                case SynthSettingsObjectOscillator.OscillatorType.Noise:
                    return Noise() * settings.amplitude;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetNote(int note, int sampleRate)
        {
            _isActive = true;
            _currentNote = note + settings.tuning;
            set_freq(AnywhenSynthVoice.FreqTab[_currentNote & 0x7f], sampleRate);
        }

        public void SetPitchMod(float amount, int sampleRate)
        {
            _pitchModAmount = Remap(amount, -1, 1, 0.5f, 2);
            set_freq(AnywhenSynthVoice.FreqTab[_currentNote & 0x7f], sampleRate);
        }

        public void SetFineTuning(float amount, int sampleRate)
        {
            _fineTune = amount;
            set_freq(AnywhenSynthVoice.FreqTab[_currentNote & 0x7f], sampleRate);
        }

        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }


        private void set_freq(float freq__hz, int sample_rate)
        {
            float freq__ppsmp = ((freq__hz * _pitchModAmount) + _fineTune) / sample_rate; // periods per sample
            _freqPhPSmp = (uint)(freq__ppsmp * PHASE_MAX);
        }

        /// Basic oscillators
        /// <returns></returns>
        // Library sine
        private float Sin()
        {
            float ph01 = _phase / PHASE_MAX;
            //return (float)SinT(ph01 * 6.28318530717959f, 10) * _amp;
            return Mathf.Sin(ph01 * 6.28318530717959f) * _amp;
        }

        float SineTable()
        {
            float ph01 = _phase / PHASE_MAX;
            return _sine[(int)(ph01 * _sine.Length)];
        }

        float SineTaylor()
        {
            float ph01 = _phase / PHASE_MAX;

            return (float)SinT(ph01 * 6.28318530717959f, 20) * _amp;
        }

        float SineChebyshev()
        {
            float ph01 = _phase / PHASE_MAX;
            return (float)SinC(ph01 * 6.28318530717959f, 20) * _amp;
        }

        double SinT(double x, int numTerms)
        {
            double result = 0.0;
            double term = x;

            for (int n = 1; n <= numTerms; n++)
            {
                result += term;
                term *= -x * x / ((2 * n) * (2 * n + 1));
            }

            return result;
        }


        public static double SinC(double x, int numTerms)
        {
            // Ensure x is in the range [-1, 1] for Chebyshev approximation
            x = Math.Max(-1.0, Math.Min(1.0, x));

            double result = 0.0;
            for (int n = 0; n < numTerms; n++)
            {
                double term = ChebyshevPolynomial(n, x);
                result += term;
            }

            return result;
        }

        // Chebyshev polynomial of the first kind
        private static double ChebyshevPolynomial(int n, double x)
        {
            if (n == 0)
                return 1.0;
            else if (n == 1)
                return x;
            else
                return 2.0 * x * ChebyshevPolynomial(n - 1, x) - ChebyshevPolynomial(n - 2, x);
        }

        // Differentiated Polynomial Waveform (DPW)
        // Based on Valimaki & Huovilainen: 'Oscillator and Filter Algorithms for Virtual Analog Synthesis'
        // 2nd degree, meaning that the polynomial is 2nd degree
        // public float sawDPW()
        // {
        //     if (is_active == false) return 0.0f;
        //     float ph01 = phase / PHASE_MAX;
        //     float bphase = 2.0f * ph01 - 1.0f;  // phasor in [-1;+1]       : saw
        //     float sq = bphase * bphase;         // squared saw             : parabola
        //     float dsq = sq - z1;                // differentiated parabola : bandlimited saw
        //     z1 = sq;                            // store next frame's z^-1
        //     return dsq * dpwScale * amp;
        // }
        // float z1 = 0;
        // float dpwScale = 1.0f;

        /// PolyBLEP oscillators
        // Polynomial Band-Limited Step Function (PolyBLEP)
        // Based on Valimaki 2007: 'Antialiasing Oscillators in Subtractive Synthesis'
        // and https://steemit.com/ableton/@metafunction/all-about-digital-oscillators-part-2-blits-and-bleps
        public float sawPolyBLEP()
        {
            float ph01 = _phase / PHASE_MAX;
            float result = 2.0f * ph01 - 1.0f; // phasor in [-1;+1] : saw

            result -= polyBLEP(ph01);
            return result;
        }

        // FIXME: DC offset when pulseWidth != 0.5, should be fixable by a simple offset
        public float squarePolyBLEP(float pulseWidth)
        {
            float ph01 = _phase / PHASE_MAX;

            float value;
            if (ph01 < pulseWidth)
            {
                value = _amp;
            }
            else
            {
                value = -_amp;
            }

            value += polyBLEP(ph01); // Layer output of Poly BLEP on top (flip)
            value -= polyBLEP((ph01 + 1.0f - pulseWidth) % 1.0f); // Layer output of Poly BLEP on top (flop)

            return value;
        }

        private float polyBLEP(float t)
        {
            // phase step in [0;1]
            float dt = _freqPhPSmp / PHASE_MAX;

            // t-t^2/2 +1/2
            // 0 < t <= 1
            // discontinuities between 0 & 1
            if (t < dt) // one sample width at the start of period
            {
                t /= dt;
                return t + t - t * t - 1.0f;
            }

            // t^2/2 +t +1/2
            // -1 <= t <= 0
            // discontinuities between -1 & 0
            if (t > 1.0f - dt) // one sample width at the end of period
            {
                t = (t - 1.0f) / dt;
                return t * t + t + t + 1.0f;
            }

            return 0.0f;
        }


        private float WaveTable()
        {
            float ph01 = _phase / PHASE_MAX;
            switch (settings.waveTableOscillatorType)
            {
                case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Sine8Bit:
                    return _sine8Bit[(int)(ph01 * (_sine8Bit.Length - 1))];
                case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Saw8Bit:
                    return _saw8Bit[(int)(ph01 * (_saw8Bit.Length - 1))];
                case SynthSettingsObjectOscillator.WaveTableOscillatorTypes.Square8Bit:
                    int val = (int)(ph01 * (_square8Bit.Length - 1));
                    if (val != 0 && val != 1)
                    {
                        Debug.Log(val);
                    }

                    return _square8Bit[(int)(ph01 * (_square8Bit.Length - 1))];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        float Noise()
        {
            float ph01 = _phase / PHASE_MAX;
            return _noiseWhite[(int)(ph01 * (_noiseWhite.Length - 1))];
        }

        public void UpdateSettings(SynthSettingsObjectOscillator newSettings)
        {
            this.settings = newSettings;
        }

        public void SetInactive()
        {
            _isActive = false;
        }
    }
}