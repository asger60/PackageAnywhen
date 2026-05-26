using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;


public struct AnywhenAudioVoice
{
    private double _scheduledStartTime, _scheduledEndTime;
    private bool _noteOn;
    private bool _noteQueued;
    private float _velocity;
    AudioProcessorEnvelope _voiceEnvelope;

    private AudioProcessorSettings.EnvelopeSettings _envelopeSettings;
    private NativeArray<ModRouting> _pitchMod;
    public bool IsIdle => !_noteQueued && !_isPlaying;
    public double ScheduledStartTime => _scheduledStartTime;
    private float _trackPitch;
    private NativeArray<TrackAudioSource> _audioSources;
    int _sampleRate;
    NativeArray<AudioSourceSettings.Unmanaged> _audioSourceSettings;
    private bool _isPlaying;

    public void Init(int sampleRate)
    {
        _sampleRate = sampleRate;
        _voiceEnvelope = new AudioProcessorEnvelope(sampleRate);
        _trackPitch = 1;
        if (_audioSources.IsCreated) _audioSources.Dispose();
    }

    public void UpdateVoiceSettings(
        NativeArray<AudioSourceSettings.Unmanaged> audioSourceSettings,
        AudioProcessorSettings.EnvelopeSettings envelopeSettings,
        NativeArray<ModRouting> pitchMod,
        float trackPitch)
    {
        if (!_audioSourceSettings.Equals(audioSourceSettings))
            _audioSourceSettings = audioSourceSettings;

        if (!_envelopeSettings.Equals(envelopeSettings))
            _envelopeSettings = envelopeSettings;

        if (!_pitchMod.Equals(pitchMod))
            _pitchMod = pitchMod;

        if (!Mathf.Approximately(_trackPitch, trackPitch))
            _trackPitch = trackPitch;

        for (int i = 0; i < _audioSources.Length; i++)
        {
            if (i >= audioSourceSettings.Length)
            {
                Debug.LogWarning("Audio source settings array is too short");
                break;
            }

            _audioSources[i].UpdateSettings(audioSourceSettings[i]);
        }
    }

    public void RecreateVoice(NativeArray<AudioSourceSettings.Unmanaged> audioSourceSettings)
    {
        if (_audioSources.IsCreated) _audioSources.Dispose();
        _audioSourceSettings = audioSourceSettings;
        _audioSources = new NativeArray<TrackAudioSource>(audioSourceSettings.Length, Allocator.Persistent);
        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[i] = new TrackAudioSource(_sampleRate, audioSourceSettings[i]);
        }
    }

    internal float Process(double dspTime, in AnysongTrack anysongTrack)
    {
        float clipAmplitude = 0;
        if (_noteQueued && dspTime >= _scheduledStartTime)
        {
            _isPlaying = true;
            _noteQueued = false;
        }

        if (_isPlaying)
        {
            _voiceEnvelope.SetGate(true);
            float pitchModSignal = anysongTrack.GetModSignal(_pitchMod);
            float pitchMultiplier = _trackPitch * (float)Math.Pow(2.0, pitchModSignal);

            if (_audioSources.IsCreated)
            {
                for (int i = 0; i < _audioSources.Length; i++)
                {
                    var audioSource = _audioSources[i];
                    clipAmplitude += audioSource.Process(clipAmplitude, pitchMultiplier);
                    _audioSources[i] = audioSource;
                }
            }

            if (dspTime >= _scheduledEndTime)
            {
                _voiceEnvelope.SetGate(false);
                if (!_voiceEnvelope.IsActive)
                    _isPlaying = false;
            }
        }

        //bool envelopeActive = _voiceEnvelope.IsActive;
        //if (!isPlaying && !envelopeActive)
        //{
        //    return 0;
        //}

        //_voiceEnvelope.SetGate(dspTime >= _scheduledStartTime && dspTime <= _scheduledEndTime);

        return clipAmplitude * _velocity * _voiceEnvelope.Process((float)dspTime, anysongTrack);
    }


    internal void QueueNote(AnywhenAudioGenerator.PlaybackEvent playbackEvent)
    {
        _voiceEnvelope.SetSettings(_envelopeSettings);
        _voiceEnvelope.Reset();
        _noteQueued = true;
        _scheduledStartTime = playbackEvent.ScheduledPlayTime;
        _scheduledEndTime = playbackEvent.ScheduledEndTime;

        if (_audioSources.IsCreated)
        {
            for (int i = 0; i < _audioSources.Length; i++)
            {
                var audioSource = _audioSources[i];
                audioSource.UpdateSettings(_audioSourceSettings[i]);
                audioSource.QueueNote(playbackEvent.Note.noteIndex);
                _audioSources[i] = audioSource;
            }
        }

        _velocity = playbackEvent.Note.velocity;
    }

    public void Dispose()
    {
        if (_audioSources.IsCreated) _audioSources.Dispose();
    }
}