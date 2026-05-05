using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;


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

    private float TrackLFO1Value => _trackLFO1Value;
    private float TrackLFO2Value => _trackLFO2Value;
    private float TrackEnvelope1Value => _trackEnvelope1Value;
    private float TrackEnvelope2Value => _trackEnvelope2Value;
    public AudioProcessorSettings.EnvelopeSettings.Unmanaged EnvelopeSettings => _settings.TrackAudioEnvelope1.ToUnmanaged();

    private bool _hasPendingTracksUpdate;
    private bool _hasPendingEffectsUpdate;
    private bool _hasPendingParameterUpdate;
    private AnysongTrackSettings.Unmanaged _pendingSettings;
    AnysongTrackSettings.Unmanaged _settings;
    private int _trackTypeIndex;
    public int TrackTypeIndex => _trackTypeIndex;
    public bool IsMute;
    private NativeArray<SynthFilterBase.ModRouting> _amplitudeMod;


    public AnysongTrack(int sampleRate, AnysongTrackSettings.Unmanaged settings) : this()
    {
        IsMute = false;
        _sampleRate = sampleRate;
        _trackLFO1Value = 0;
        _trackLFO2Value = 0;
        _trackTypeIndex = settings.trackTypeIndex;
        CreateTrack(settings, sampleRate);
        UpdateSettings(settings);
        _nextEvent = new AnywhenAudioGenerator.PlaybackEvent(new SimpleNoteEvent(), 0);
        _amplitudeMod = settings.amplitudeMod;
    }


    public void OnTracksRebuild(AnysongTrackSettings.Unmanaged newSettings)
    {
        _pendingSettings = newSettings;
        _hasPendingTracksUpdate = true;
    }

    public void OnEffectsRebuild(AnysongTrackSettings.Unmanaged newSettings)
    {
        _pendingSettings = newSettings;
        _hasPendingEffectsUpdate = true;
        _hasPendingParameterUpdate = true;
    }

    public void OnValuesChanged(AnysongTrackSettings.Unmanaged newSettings)
    {
        _pendingSettings = newSettings;
        _hasPendingParameterUpdate = true;
    }

    public void CreateTrack(AnysongTrackSettings.Unmanaged settings, int sampleRate)
    {
        if (_voices.IsCreated) _voices.Dispose();
        if (_trackFilters.IsCreated) _trackFilters.Dispose();

        _voices = new NativeArray<AnywhenAudioVoice>(settings.voices, Allocator.Persistent);
        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            voice.Init(sampleRate);
            voice.RecreateVoice(settings.audioSources);
            _voices[i] = voice;
        }

        _trackEnvelope1 = new AudioProcessorEnvelope(_sampleRate);
        _trackEnvelope2 = new AudioProcessorEnvelope(_sampleRate);
        _trackLFO1 = new AudioProcessorLFO(_sampleRate);
        _trackLFO2 = new AudioProcessorLFO(_sampleRate);

        _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

        for (int i = 0; i < settings.trackFilters.Length; i++)
        {
            _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
        }


        _amplitudeMod = settings.amplitudeMod;
    }

    private void CreateEffects(AnysongTrackSettings.Unmanaged settings)
    {
        if (_trackFilters.IsCreated) _trackFilters.Dispose();
        _trackFilters = new NativeArray<TrackAudioProcessor>(settings.trackFilters.Length, Allocator.Persistent);

        for (int i = 0; i < settings.trackFilters.Length; i++)
        {
            _trackFilters[i] = new TrackAudioProcessor(_sampleRate, settings.trackFilters[i]);
        }
    }


    public void UpdateSettings(AnysongTrackSettings.Unmanaged settings)
    {
        _settings = settings;
        _trackVolume = settings.volume;

        _trackEnvelope1.SetSettings(settings.TrackAudioEnvelope1.ToUnmanaged());
        _trackEnvelope2.SetSettings(settings.TrackAudioEnvelope2.ToUnmanaged());

        _trackEnvelope1Value = 1;
        _trackEnvelope2Value = 1;

        _trackLFO1.SetSettings(settings.TrackAudioLFO1.ToUnmanaged());
        _trackLFO2.SetSettings(settings.TrackAudioLFO2.ToUnmanaged());

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
                    settings.TrackAudioEnvelope1.ToUnmanaged(),
                    _settings.pitchMod,
                    _settings.trackPitch);
                _voices[i] = voice;
            }
        }

        _amplitudeMod = settings.amplitudeMod;
    }

    internal void HandlePlaybackEvent(AnywhenAudioGenerator.PlaybackEvent playbackEvent)
    {
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
        if (IsMute) return 0;

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


        _trackLFO1Value = _trackLFO1.Process((float)dspTime, this);
        _trackLFO2Value = _trackLFO2.Process((float)dspTime, this);
        _trackEnvelope1Value = _trackEnvelope1.Process((float)dspTime, this);
        _trackEnvelope2Value = _trackEnvelope2.Process((float)dspTime, this);

        if (dspTime >= _nextEvent.ScheduledPlayTime && dspTime < _nextEvent.ScheduledEndTime)
        {
            _trackEnvelope1.SetGate(true);
            _trackEnvelope2.SetGate(true);
        }
        else if (dspTime >= _nextEvent.ScheduledEndTime)
        {
            _trackEnvelope1.SetGate(false);
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

    public float GetModSignal(NativeArray<SynthFilterBase.ModRouting> modRoutingSettings)
    {
        float s = 0;
        for (int i = 0; i < modRoutingSettings.Length; i++)
        {
            var mod = modRoutingSettings[i];
            float signal = mod.modSource switch
            {
                SynthFilterBase.ModRouting.ModSources.Envelope1 => TrackEnvelope1Value,
                SynthFilterBase.ModRouting.ModSources.Envelope2 => TrackEnvelope2Value,
                SynthFilterBase.ModRouting.ModSources.LFO1 => TrackLFO1Value,
                SynthFilterBase.ModRouting.ModSources.LFO2 => TrackLFO2Value,
                _ => 0f
            };

            s += signal * mod.modAmount;
        }

        return s;
    }

    public void Dispose()
    {
        if (_voices.IsCreated)
            _voices.Dispose();

        if (_trackFilters.IsCreated)
            _trackFilters.Dispose();
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