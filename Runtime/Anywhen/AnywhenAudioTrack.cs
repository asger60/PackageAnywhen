using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;


public struct AnysongTrack : IEquatable<AnysongTrack>
{
    int _sampleRate;
    private NativeArray<AnywhenAudioVoice> _voices;
    private float _trackVolume;
    private AudioProcessorEnvelope _trackEnvelope1, _trackEnvelope2;
    AudioProcessorLFO _trackLFO1, _trackLFO2;
    private float _trackLFO1Value;
    float _trackLFO2Value;
    private float _trackEnvelope1Value;
    private float _trackEnvelope2Value;

    private AnywhenAudioGenerator.PlaybackEvent _nextEvent;
    private NativeArray<TrackAudioProcessor> _trackFilters;


    private bool _hasPendingTracksUpdate;
    private bool _hasPendingEffectsUpdate;
    private bool _hasPendingParameterUpdate;
    private AnysongTrackSettings.Unmanaged _pendingSettings;
    AnysongTrackSettings.Unmanaged _settings;
    private int _trackTypeIndex;
    public int TrackTypeIndex => _trackTypeIndex;
    public bool IsMute;
    private NativeArray<ModRouting> _amplitudeMod;


    public AnysongTrack(int sampleRate, AnysongTrackSettings.Unmanaged settings) : this()
    {
        IsMute = false;
        _sampleRate = sampleRate;
        _trackLFO1Value = 0;
        _trackLFO2Value = 0;
        _trackTypeIndex = settings.trackTypeIndex;
        CreateTrack(settings, sampleRate);
        UpdateSettings(settings);
        _amplitudeMod = settings.AmplitudeMod;
    }



    public void CreateTrack(AnysongTrackSettings.Unmanaged settings, int sampleRate)
    {
        if (_voices.IsCreated)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i].Dispose();
            }

            _voices.Dispose();
        }

        if (_trackFilters.IsCreated)
        {
            foreach (var trackFilter in _trackFilters)
            {
                trackFilter.Dispose();
            }

            _trackFilters.Dispose();
        }

        _voices = new NativeArray<AnywhenAudioVoice>(settings.voices, Allocator.Persistent);
        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            voice.Init(sampleRate);
            voice.RecreateVoice(settings.audioSources);
            _voices[i] = voice;
        }

        _trackEnvelope1 = new AudioProcessorEnvelope(_sampleRate);

        if (settings.TrackAudioEnvelope2.enabled)
            _trackEnvelope2 = new AudioProcessorEnvelope(_sampleRate);
        if (settings.TrackAudioLFO1.enabled)
            _trackLFO1 = new AudioProcessorLFO(_sampleRate);
        if (settings.TrackAudioLFO2.enabled)
            _trackLFO2 = new AudioProcessorLFO(_sampleRate);

        _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

        for (int i = 0; i < settings.trackFilters.Length; i++)
        {
            _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
        }


        _amplitudeMod = settings.AmplitudeMod;
    }

    private void CreateEffects(AnysongTrackSettings.Unmanaged settings)
    {
        if (_trackFilters.IsCreated)
        {
            for (int i = 0; i < _trackFilters.Length; i++)
            {
                _trackFilters[i].Dispose();
            }

            _trackFilters.Dispose();
        }

        _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

        for (int i = 0; i < settings.trackFilters.Length; i++)
        {
            _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
        }
    }


    public void UpdateSettings(AnysongTrackSettings.Unmanaged settings)
    {
        if (_settings.Equals(settings))
        {
            return;
        }

        _settings.Dispose();
        _settings = settings;
        _trackVolume = settings.Volume;

        _trackEnvelope1.SetSettings(settings.TrackAudioEnvelope1);

        if (settings.TrackAudioEnvelope2.enabled)
            _trackEnvelope2.SetSettings(settings.TrackAudioEnvelope2);

        _trackEnvelope1Value = 1;
        _trackEnvelope2Value = 1;

        if (settings.TrackAudioLFO1.enabled)
            _trackLFO1.SetSettings(settings.TrackAudioLFO1);

        if (settings.TrackAudioLFO2.enabled)
            _trackLFO2.SetSettings(settings.TrackAudioLFO2);


        for (int i = 0; i < settings.trackFilters.Length; i++)
        {
            var filter = _trackFilters[i];
            filter.UpdateSettings(settings.trackFilters[i]);
            _trackFilters[i] = filter;
        }

        if (_voices.IsCreated)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                var voice = _voices[i];
                voice.UpdateVoiceSettings(
                    settings.audioSources,
                    settings.TrackAudioEnvelope1,
                    _settings.PitchMod,
                    _settings.trackPitch);
                _voices[i] = voice;
            }
        }

        if (!_amplitudeMod.Equals(settings.AmplitudeMod))
            _amplitudeMod = settings.AmplitudeMod;
    }

    internal void HandlePlaybackEvent(AnywhenAudioGenerator.PlaybackEvent playbackEvent)
    {
        if (IsMute) return;
        _nextEvent = playbackEvent;

        int voiceToSteal = -1;
        double oldestStartTime = double.MaxValue;

        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            if (voice.IsIdle)
            {
                voice.QueueNote(playbackEvent);
                _voices[i] = voice;
                return;
            }

            if (voice.ScheduledStartTime < oldestStartTime)
            {
                oldestStartTime = voice.ScheduledStartTime;
                voiceToSteal = i;
            }

            _voices[i] = voice;
        }

        if (voiceToSteal != -1)
        {
            var voice = _voices[voiceToSteal];
            voice.QueueNote(playbackEvent);
            _voices[voiceToSteal] = voice;
        }
    }

    internal float Process(double dspTime)
    {
        //if (IsMute) return 0;

        bool trackActive = _trackEnvelope1.IsActive || _trackEnvelope2.IsActive;
        bool anyVoiceActive = false;

        if (_voices.IsCreated)
        {
            foreach (var voice in _voices)
            {
                if (voice.IsIdle) continue;
                anyVoiceActive = true;
                break;
            }
        }

        if (!trackActive && !anyVoiceActive && dspTime >= _nextEvent.ScheduledEndTime)
        {
            return 0;
        }

        if (_hasPendingTracksUpdate)
        {
            CreateTrack(_pendingSettings, _sampleRate);
            _hasPendingTracksUpdate = false;
        }

        if (_hasPendingEffectsUpdate)
        {
            CreateEffects(_pendingSettings);
            _hasPendingEffectsUpdate = false;
        }

        if (_hasPendingParameterUpdate)
        {
            UpdateSettings(_pendingSettings);
            _hasPendingParameterUpdate = false;
        }


        if (!_voices.IsCreated)
            return 0;

        float clipAmplitude = 0;

        float fTime = (float)dspTime;
        if (_settings.TrackAudioLFO1.enabled)
            _trackLFO1Value = _trackLFO1.Process(fTime, this);
        else
            _trackLFO1Value = 0;

        if (_settings.TrackAudioLFO2.enabled)
            _trackLFO2Value = _trackLFO2.Process(fTime, this);
        else
            _trackLFO2Value = 0;

        if (_settings.TrackAudioEnvelope1.enabled)
            _trackEnvelope1Value = _trackEnvelope1.Process(fTime, this);
        else
            _trackEnvelope1Value = 1;

        if (_settings.TrackAudioEnvelope2.enabled)
            _trackEnvelope2Value = _trackEnvelope2.Process(fTime, this);
        else
            _trackEnvelope2Value = 1;

        if (dspTime >= _nextEvent.ScheduledPlayTime && dspTime < _nextEvent.ScheduledEndTime)
        {
            if (_settings.TrackAudioEnvelope1.enabled)
                _trackEnvelope1.SetGate(true);
            if (_settings.TrackAudioEnvelope2.enabled)
                _trackEnvelope2.SetGate(true);
        }
        else if (dspTime >= _nextEvent.ScheduledEndTime)
        {
            if (_settings.TrackAudioEnvelope1.enabled)
                _trackEnvelope1.SetGate(false);
            if (_settings.TrackAudioEnvelope2.enabled)
                _trackEnvelope2.SetGate(false);
        }


        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            clipAmplitude += voice.Process(dspTime, this);
            _voices[i] = voice;
        }

        clipAmplitude *= 1 + GetModSignal(_amplitudeMod);

        if (_trackFilters.IsCreated)
        {
            for (int i = 0; i < _trackFilters.Length; i++)
            {
                var filter = _trackFilters[i];
                clipAmplitude = filter.Process(clipAmplitude, this);
                _trackFilters[i] = filter;
            }
        }

        return clipAmplitude * _trackVolume;
    }

    public float GetModSignal(NativeArray<ModRouting> modRoutingSettings)
    {
        if (!modRoutingSettings.IsCreated || modRoutingSettings.Length == 0) return 0;
        float s = 0;
        foreach (var mod in modRoutingSettings)
        {
            float signal = mod.modSource switch
            {
                ModRouting.ModSources.Envelope1 => _trackEnvelope1Value,
                ModRouting.ModSources.Envelope2 => _trackEnvelope2Value,
                ModRouting.ModSources.LFO1 => _trackLFO1Value,
                ModRouting.ModSources.LFO2 => _trackLFO2Value,
                _ => 0f
            };

            s += signal * mod.modAmount;
        }

        return s;
    }

    public void Dispose()
    {
        _settings.Dispose();

        if (_voices.IsCreated)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i].Dispose();
            }

            _voices.Dispose();
        }

        if (_trackFilters.IsCreated)
        {
            for (int i = 0; i < _trackFilters.Length; i++)
            {
                _trackFilters[i].Dispose();
            }

            _trackFilters.Dispose();
        }
    }

    public bool Equals(AnysongTrack other)
    {
        return _voices.Equals(other._voices) &&
               _trackVolume.Equals(other._trackVolume) && _trackEnvelope1.Equals(other._trackEnvelope1) &&
               _nextEvent.Equals(other._nextEvent);
    }

    public override bool Equals(object obj)
    {
        return obj is AnysongTrack other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_voices, _trackVolume, _trackEnvelope1, _nextEvent);
    }
}