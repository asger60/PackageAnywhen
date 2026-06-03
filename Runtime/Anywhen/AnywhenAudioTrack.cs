using System;
using System.ComponentModel;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;


public struct AnysongTrack : IEquatable<AnysongTrack>
{
    int _sampleRate;
    private NativeArray<AnywhenAudioVoice> _voices;
    private float _trackVolume;
    private AudioProcessorEnvelope _trackEnvelope1, _trackEnvelope2;
    AudioProcessorLFO _trackLFO1, _trackLFO2;
    private NativeArray<float> _trackLFO1Value;
    private NativeArray<float> _trackLFO2Value;
    private NativeArray<float> _trackEnvelope1Value;
    private NativeArray<float> _trackEnvelope2Value;

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
    private NativeArray<float> _nullBuffer;
    private NativeArray<float> _tempModBuffer;
    private NativeArray<float> _subMixBuffer;


    public AnysongTrack(int sampleRate, AnysongTrackSettings.Unmanaged settings, int blockSize) : this()
    {
        IsMute = false;
        _sampleRate = sampleRate;
        _trackLFO1Value = new NativeArray<float>(blockSize, Allocator.Persistent);
        _trackLFO2Value = new NativeArray<float>(blockSize, Allocator.Persistent);
        _trackEnvelope1Value = new NativeArray<float>(blockSize, Allocator.Persistent);
        _trackEnvelope2Value = new NativeArray<float>(blockSize, Allocator.Persistent);
        _nullBuffer = new NativeArray<float>(blockSize, Allocator.Persistent);
        _tempModBuffer = new NativeArray<float>(blockSize, Allocator.Persistent);
        _subMixBuffer = new NativeArray<float>(blockSize, Allocator.Persistent);
        _trackTypeIndex = settings.trackTypeIndex;
        CreateTrack(settings, sampleRate, blockSize);
        UpdateSettings(settings, blockSize);
        _amplitudeMod = settings.AmplitudeMod;
    }


    public void CreateTrack(AnysongTrackSettings.Unmanaged settings, int sampleRate, int blockSize)
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
            voice.Init(sampleRate, blockSize);
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


    public void UpdateSettings(AnysongTrackSettings.Unmanaged settings, int blockSize)
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

        for (int frame = 0; frame < blockSize; frame++)
        {
            _trackEnvelope1Value[frame] = 1;
            _trackEnvelope2Value[frame] = 1;
        }

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

    internal void Process(double dspTime, double inverseSampleRate, NativeArray<float> channelBuffer, int blockSize) 
    {
       

        if (_hasPendingTracksUpdate)
        {
            CreateTrack(_pendingSettings, _sampleRate, blockSize);
            _hasPendingTracksUpdate = false;
        }

        if (_hasPendingEffectsUpdate)
        {
            CreateEffects(_pendingSettings);
            _hasPendingEffectsUpdate = false;
        }

        if (_hasPendingParameterUpdate)
        {
            UpdateSettings(_pendingSettings, blockSize);
            _hasPendingParameterUpdate = false;
        }




        for (int frame = 0; frame < channelBuffer.Length; frame++)
        {

            double fTime = dspTime + (frame * inverseSampleRate);
            if (_settings.TrackAudioLFO1.enabled)
                _trackLFO1Value[frame] = _trackLFO1.Process(this);
            else
                _trackLFO1Value[frame] = 0;

            if (_settings.TrackAudioLFO2.enabled)
                _trackLFO2Value[frame] = _trackLFO2.Process(this);
            else
                _trackLFO2Value[frame] = 0;

            if (_settings.TrackAudioEnvelope1.enabled)
                _trackEnvelope1Value[frame] = _trackEnvelope1.Process((float)fTime, this);
            else
                _trackEnvelope1Value[frame] = 1;

            if (_settings.TrackAudioEnvelope2.enabled)
                _trackEnvelope2Value[frame] = _trackEnvelope2.Process((float)fTime, this);
            else
                _trackEnvelope2Value[frame] = 1;

            if (fTime >= _nextEvent.ScheduledPlayTime && fTime < _nextEvent.ScheduledEndTime)
            {
                if (_settings.TrackAudioEnvelope1.enabled)
                    _trackEnvelope1.SetGate(true);
                if (_settings.TrackAudioEnvelope2.enabled)
                    _trackEnvelope2.SetGate(true);
            }
            else if (fTime >= _nextEvent.ScheduledEndTime)
            {
                if (_settings.TrackAudioEnvelope1.enabled)
                    _trackEnvelope1.SetGate(false);
                if (_settings.TrackAudioEnvelope2.enabled)
                    _trackEnvelope2.SetGate(false);
            }
        }

        for (int frame = 0; frame < _subMixBuffer.Length; frame++)
        {
            _subMixBuffer[frame] = 0;
        }

        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            voice.Process(dspTime, inverseSampleRate, this, _subMixBuffer);
            _voices[i] = voice;
        }



        if (_trackFilters.IsCreated)
        {
            for (int i = 0; i < _trackFilters.Length; i++)
            {
                var filter = _trackFilters[i];
                filter.Process(_subMixBuffer, this);
                _trackFilters[i] = filter;
            }
        }

        for (int frame = 0; frame < _subMixBuffer.Length; frame++)
        {
            channelBuffer[frame] += _subMixBuffer[frame] * _trackVolume;
        }

    }

    public readonly void CalculateModSignal(NativeArray<ModRouting> modRoutingSettings, NativeArray<float> modBuffer)
    {
        for (int frame = 0; frame < modBuffer.Length; frame++)
        {
            modBuffer[frame] = 0;
        }


        if (!modRoutingSettings.IsCreated || modRoutingSettings.Length == 0) return;

        foreach (var mod in modRoutingSettings)
        {
            NativeArray<float> signal = mod.modSource switch
            {
                ModRouting.ModSources.Envelope1 => _trackEnvelope1Value,
                ModRouting.ModSources.Envelope2 => _trackEnvelope2Value,
                ModRouting.ModSources.LFO1 => _trackLFO1Value,
                ModRouting.ModSources.LFO2 => _trackLFO2Value,
                _ => _nullBuffer
            };

            for (int frame = 0; frame < modBuffer.Length; frame++)
            {
                modBuffer[frame] += signal[frame] * mod.modAmount;
            }
        }
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

        _trackLFO1Value.Dispose();
        _trackLFO2Value.Dispose();
        _trackEnvelope1Value.Dispose();
        _trackEnvelope2Value.Dispose();
        _nullBuffer.Dispose();
        _tempModBuffer.Dispose();
        _subMixBuffer.Dispose();
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