using System;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using UnityEngine;


public class SynthControlLFO : SynthControlBase
{
    private UInt32 _phase = 0u; // using an integer type automatically ensures limits

    private const float PhaseMax = 4294967296;
    private float _currentAmp;
    private UInt32 _freqPhPSmp = 0u;
    private bool _isActive = false;
    private float _fadeInStart, _fadeInEnd;

    private float _fadeInDuration = 0.01f;

    //private SynthSettingsObjectLFO _settings;
    private float _currentFrequency;
    private float _sendAmount;
    bool _retrigger;

    public void UpdateSettings(AnywhenSampleInstrument.PitchLFOSettings newSettings)
    {
        _currentFrequency = newSettings.frequency;
        SetFreq(newSettings.frequency);
        _isActive = true;
        //_phase = 0u;
        _currentAmp = newSettings.amplitude;
        _retrigger = newSettings.retrigger;
        _sendAmount = 100;
        //Restart();
        //_fadeInStart = (float)AudioSettings.dspTime;
        //_fadeInEnd = (float)AudioSettings.dspTime + _settings.fadeInDuration;
    }

    public void UpdateSettings(SynthSettingsObjectLFO settingsObject)
    {
        //_settings = settingsObject;
        _isActive = true;
        _phase = 0u;
        _currentAmp = 1;
        _fadeInStart = (float)AudioSettings.dspTime;
        _fadeInEnd = (float)AudioSettings.dspTime + settingsObject.fadeInDuration;
        _currentFrequency = settingsObject.frequency;
        SetFreq(settingsObject.frequency);
        _retrigger = settingsObject.retrigger;
        _sendAmount = settingsObject.sendAmount;
    }


    private void Restart()
    {
        if (!_isActive) return;
        _phase = 0u;
        _isActive = true;
        _fadeInStart = (float)AudioSettings.dspTime;
        _fadeInEnd = (float)AudioSettings.dspTime + _fadeInDuration;
        SetFreq(_currentFrequency);
    }


    public override void DoUpdate()
    {
        //if (!_isActive) return;
        //_currentAmp = Mathf.InverseLerp(_fadeInStart, _fadeInEnd, (float)AudioSettings.dspTime);
        _phase += _freqPhPSmp;
    }


    public override void NoteOn()
    {
        if (_retrigger)
            Restart();
    }

    private void SetFreq(float freqHz, int sampleRate = 48000)
    {
        float freqPpsmp = freqHz / sampleRate; // periods per sample
        _freqPhPSmp = (uint)(freqPpsmp * PhaseMax);
    }

    public override float Process(bool unipolar = false)
    {
        if (unipolar)
            return Sin();

        return 1 + Sin() * _currentAmp * (_sendAmount / 100f);
    }

    /// Basic oscillators
    /// <returns></returns>
    // Library sine
    // - possibly slow
    private float Sin()
    {
        if (!_isActive) return 0.0f;
        float ph01 = _phase / PhaseMax;
        return Mathf.Sin(ph01 * 6.28318530717959f);
    }
}