using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthControlEnvelope : SynthControlBase
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


        public AnywhenSampleInstrument.EnvelopeSettings Settings;

        public override void NoteOn()
        {
            //Reset();
            SetAttackRate(Settings.attack * AnywhenRuntime.SampleRate);
            SetDecayRate(Settings.decay * AnywhenRuntime.SampleRate);
            SetSustainLevel(Settings.sustain);
            SetReleaseRate(Settings.release * AnywhenRuntime.SampleRate);
            SetTargetRatioA(0.3f);
            SetTargetRatioDr(0.3f);
            _state = EnvState.env_attack;
        }

        public override void NoteOff()
        {
            SetReleaseRate(Settings.release * AnywhenRuntime.SampleRate);
            SetTargetRatioA(0.3f);
            SetTargetRatioDr(0.3f);
            _state = EnvState.env_release;
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
        }

        private float CalcCoef(float rate, float targetRatio)
        {
            if (rate <= 0 || targetRatio <= 0) return 0;
            return Mathf.Exp(-Mathf.Log((1.0f + targetRatio) / targetRatio) / rate);
        }


        public override float Process(bool unipolar = false)
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

            return Mathf.Clamp01(_output);
        }

        public void UpdateSettings(AnywhenSampleInstrument.EnvelopeSettings newSettings)
        {
            Settings = newSettings;
        }

        public void UpdateSettings(SynthSettingsObjectEnvelope newSettings)
        {
            Settings = new AnywhenSampleInstrument.EnvelopeSettings(newSettings.attack, newSettings.decay, newSettings.sustain, newSettings.release);
        }
    }
}