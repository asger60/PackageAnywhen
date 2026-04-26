// Copyright (c) 2018 Jakob Schmid
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE."

// Huovilainen moog filter:
// [... header comments unchanged ...]

using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorLowPass : IAudioProcessor
    {
        const float V_t = 1.22070313f;

        private float _modOffsetSemitones; // accumulated semitone offset from all mod sources
        private float _modulatedCutoff; // final cutoff after modulation applied

        float _resonance;
        int _oversampling;

        float y_a, y_b, y_c, y_d;
        float w_a, w_b, w_c;

        float _s, _v;
        private float _cutoff;
        private int _sampleRate;
        AudioProcessorSettingsObject.LowPassSettings.Unmanaged _settings;

        public AudioProcessorLowPass(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
            _v = 1.0f / (2.0f * V_t);
            _modOffsetSemitones = 0f;
            _oversampling = 1;
        }

        public void DoUpdate()
        {
        }

        public void SetSettings(AudioProcessorSettingsObject.Unmanaged settings)
        {
            _settings = settings.lowPassSettings;
            _resonance = _settings.resonance;
            _cutoff = _settings.cutoffFrequency;
            _oversampling = _settings.oversampling;
            RecalculateS();
        }

        public float Process(float sample, AnywhenAudioGenrator.Processor.Track track)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            if (_settings.cutoffMod is { IsCreated: true, Length: > 0 })
            {
                _modOffsetSemitones = track.GetModSignal(_settings.cutoffMod);
                _modulatedCutoff = _cutoff * Mathf.Pow(2f, _modOffsetSemitones / 12f);
                _modulatedCutoff = Clamp(_modulatedCutoff, 20f, 20000f);
                RecalculateS();
            }

            float input = sample;

            for (int j = 0; j < _oversampling; ++j)
            {
                float resonanceFeedback = 4.0f * _resonance * y_d;
                float x = input - resonanceFeedback;

                y_a += _s * (FastTanh(x * _v) - w_a);
                w_a = FastTanh(y_a * _v);

                y_b += _s * (w_a - w_b);
                w_b = FastTanh(y_b * _v);

                y_c += _s * (w_b - w_c);
                w_c = FastTanh(y_c * _v);

                y_d += _s * (w_c - FastTanh(y_d * _v));

                if (float.IsNaN(y_d) || float.IsInfinity(y_d))
                {
                    y_a = y_b = y_c = y_d = 0;
                    w_a = w_b = w_c = 0;
                    break;
                }
            }

            float outSample = y_d * (1.0f + _resonance * 4.0f);
            return Clamp(outSample, -1f, 1f);
        }

        public void SetGate(bool gate)
        {
        }

        private static float FastTanh(float x)
        {
            if (x < -3) return -1;
            if (x > 3) return 1;
            float x2 = x * x;
            return x * (27 + x2) / (27 + 9 * x2);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private void RecalculateS()
        {
            if (_sampleRate <= 0) return;

            float compensation = 0.435f;
            // Use _modulatedCutoff when mod is active, otherwise fall back to the raw cutoff
            float f = (_modulatedCutoff > 0 ? _modulatedCutoff : _cutoff) * compensation;
            float omega = 2.0f * Mathf.PI * f / (_sampleRate * _oversampling);

            _s = 1.0f - Mathf.Exp(-omega);
            _s = Clamp(_s, 0f, 1.0f);
        }
    }
}