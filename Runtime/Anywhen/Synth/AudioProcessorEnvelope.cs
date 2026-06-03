using System;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorEnvelope 
    {
        private enum EnvState
        {
            env_idle = 0,
            env_attack,
            env_decay,
            env_sustain,
            env_release
        };

        private EnvState _state;
        private float _output;
        private float _attackRate;
        private float _decayRate;
        private float _releaseRate;
        private float _attackCoef;
        private float _decayCoef;
        private float _releaseCoef;
        private float _sustainLevel;
        private float _targetRatioA;
        private float _targetRatioDr;
        private float _attackBase;
        private float _decayBase;
        private float _releaseBase;


        private AudioProcessorSettings.EnvelopeSettings _settings;
        private readonly int _sampleRate;

        public bool IsActive => _state != EnvState.env_idle;
        private bool _currentGate;
        


        public AudioProcessorEnvelope(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
        }


        private void SetAttackRate(float rate)
        {
            _attackRate = rate;
            _attackCoef = CalcCoef(rate, _targetRatioA);
            _attackBase = (1.0f + _targetRatioA) * (1.0f - _attackCoef);
        }

        private void SetDecayRate(float rate)
        {
            _decayRate = rate;
            _decayCoef = CalcCoef(rate, _targetRatioDr);
            _decayBase = (_sustainLevel - _targetRatioDr) * (1.0f - _decayCoef);
        }

        private void SetReleaseRate(float rate)
        {
            _releaseRate = rate;
            _releaseCoef = CalcCoef(rate, _targetRatioDr);
            _releaseBase = -_targetRatioDr * (1.0f - _releaseCoef);
        }

        private void SetSustainLevel(float level)
        {
            _sustainLevel = level;
            _decayBase = (_sustainLevel - _targetRatioDr) * (1.0f - _decayCoef);
        }

        private void SetTargetRatioA(float targetRatio)
        {
            if (targetRatio < 0.000000001f)
                targetRatio = 0.000000001f; // -180 dB
            _targetRatioA = targetRatio;
            _attackCoef = CalcCoef(_attackRate, _targetRatioA);
            _attackBase = (1.0f + _targetRatioA) * (1.0f - _attackCoef);
        }

        private void SetTargetRatioDr(float targetRatio)
        {
            if (targetRatio < 0.000000001f)
                targetRatio = 0.000000001f; // -180 dB
            _targetRatioDr = targetRatio;
            _decayCoef = CalcCoef(_decayRate, _targetRatioDr);
            _releaseCoef = CalcCoef(_releaseRate, _targetRatioDr);
            _decayBase = (_sustainLevel - _targetRatioDr) * (1.0f - _decayCoef);
            _releaseBase = -_targetRatioDr * (1.0f - _releaseCoef);
        }

        public void Reset()
        {
            _state = EnvState.env_idle;
            _output = 0.0f;
            _currentGate = false;
        }

        private float CalcCoef(float rate, float targetRatio)
        {
            if (rate <= 0 || targetRatio <= 0) return 0;
            return Mathf.Exp(-Mathf.Log((1.0f + targetRatio) / targetRatio) / rate);
        }


        public void DoUpdate()
        {
        }


        public void SetGate(bool gate)
        {
            if (gate == _currentGate) return;
            if (gate)
            {
                NoteOn();
            }
            else
            {
                NoteOff();
            }

            _currentGate = gate;
        }

        public void NoteOn()
        {
            SetTargetRatioA(0.3f);
            SetTargetRatioDr(0.3f);
            SetAttackRate(_settings.attack * _sampleRate);
            SetDecayRate(_settings.decay * _sampleRate);
            SetSustainLevel(_settings.sustain);
            SetReleaseRate(_settings.release * _sampleRate);

            _state = EnvState.env_attack;
        }

        public void NoteOff()
        {
            SetTargetRatioA(0.3f);
            SetTargetRatioDr(0.3f);
            SetAttackRate(_settings.attack * _sampleRate);
            SetDecayRate(_settings.decay * _sampleRate);
            SetSustainLevel(_settings.sustain);
            SetReleaseRate(_settings.release * _sampleRate);

            _state = EnvState.env_release;
        }

        public void SetSettings(AudioProcessorSettings.EnvelopeSettings settings)
        {
            if (_settings.Equals(settings))
            {
                return;
            }

            _settings = settings;
        }

        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
        }

        public float Process(float sample, AnysongTrack anysongTrack)
        {
            return HandleEnvelope();
        }

        public void Dispose()
        {
        }

        public float HandleEnvelope()
        {
            switch (_state)
            {
                case EnvState.env_idle:
                    break;
                case EnvState.env_attack:
                    _output = _attackBase + _output * _attackCoef;
                    if (_output >= 1.0f)
                    {
                        _output = 1.0f;
                        _state = EnvState.env_decay;
                    }

                    break;
                case EnvState.env_decay:
                    _output = _decayBase + _output * _decayCoef;
                    if (_output <= _sustainLevel)
                    {
                        _output = _sustainLevel;
                        _state = EnvState.env_sustain;
                    }

                    break;
                case EnvState.env_sustain:
                    
                    break;
                case EnvState.env_release:
                    _output = _releaseBase + _output * _releaseCoef;
                    if (_output <= 0.0f)
                    {
                        _output = 0.0f;
                        _state = EnvState.env_idle;
                    }

                    break;
            }

            return (_output);
        }

        public bool Equals(AudioProcessorEnvelope other)
        {
            return _state == other._state && _output.Equals(other._output) && _settings.Equals(other._settings);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioProcessorEnvelope other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)_state, _output, _settings);
        }
    }
}