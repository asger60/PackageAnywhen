using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;


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
    NativeArray<float> _pitchModBuffer;

    NativeArray<float> _subMix;

    public void Init(int sampleRate, int blockSize)
    {
        _sampleRate = sampleRate;
        _voiceEnvelope = new AudioProcessorEnvelope(sampleRate);
        _trackPitch = 1;
        _pitchModBuffer = new NativeArray<float>(blockSize, Allocator.Persistent);
        _subMix = new NativeArray<float>(blockSize, Allocator.Persistent);
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

    internal void Process(double dspTime, double inverseSampleRate, in AnysongTrack anysongTrack, NativeArray<float> channelBuffer)
    {
        if (_noteQueued && dspTime + (float)(_pitchModBuffer.Length * inverseSampleRate) >= _scheduledStartTime)
        {
            _isPlaying = true;
            _noteQueued = false;
        }

        if (_isPlaying)
        {
            _voiceEnvelope.SetGate(true);
        }


        if (_audioSources.IsCreated && _isPlaying)
        {
            anysongTrack.CalculateModSignal(_pitchMod, _pitchModBuffer);

            for (int frame = 0; frame < _pitchModBuffer.Length; frame++)
            {
                _pitchModBuffer[frame] = _trackPitch * (float)Math.Pow(2.0, _pitchModBuffer[frame]);
            }

            for (int frame = 0; frame < _subMix.Length; frame++)
            {
                _subMix[frame] = 0;
            }

            for (int i = 0; i < _audioSources.Length; i++)
            {
                var audioSource = _audioSources[i];

                //double currentFrameDspTime = dspTime + (frame * inverseSampleRate);
                audioSource.Process(_pitchModBuffer, dspTime, inverseSampleRate, _subMix);
                _audioSources[i] = audioSource;
            }

            float gain = (_voiceEnvelope.IsActive) ? _velocity : 0;

            for (int frame = 0; frame < channelBuffer.Length; frame++)
            {
                channelBuffer[frame] += _subMix[frame] * gain *
                                        _voiceEnvelope.Process((float)dspTime + (float)(frame * inverseSampleRate), anysongTrack);

                
                if (dspTime + (float)(frame * inverseSampleRate) >= _scheduledEndTime)
                {
                    _voiceEnvelope.SetGate(false);
                    if (!_voiceEnvelope.IsActive)
                        _isPlaying = false;
                }
            }
        }


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

        _isPlaying = false;
        _velocity = playbackEvent.Note.velocity;
    }

    public void Dispose()
    {
        if (_audioSources.IsCreated) _audioSources.Dispose();
        if (_pitchModBuffer.IsCreated) _pitchModBuffer.Dispose();
        if (_subMix.IsCreated) _subMix.Dispose();
    }
}