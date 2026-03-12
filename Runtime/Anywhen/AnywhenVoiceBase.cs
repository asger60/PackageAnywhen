using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    [Serializable]
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady => _playbackQueue.Count == 0 && !IsPlaying;
        protected AnysongTrack CurrentTrack;
        protected ADSR AmplitudeEnvelope;
        protected bool IsPlaying;
        public bool HasScheduledPlay => _playbackQueue.Count > 0;
        private List<PlaybackSettings> _playbackQueue = new ();
        protected SynthControlLFO PitchLFO;
        protected double CurrentPitch;
        protected float CurrentSampleRate;


        [Serializable]
        public struct PlaybackSettings
        {
            [FormerlySerializedAs("PlayTime")] public double playTime;
            [FormerlySerializedAs("StopTime")] public double stopTime;
            [FormerlySerializedAs("Volume")] public float volume;
            [FormerlySerializedAs("Note")] public int note;

        }

        protected PlaybackSettings CurrentPlaybackSettings;

        protected AnywhenVoiceBase(AnywhenInstrument instrumentSettings, AnysongTrack trackSettings)
        {
            CurrentSampleRate = AudioSettings.outputSampleRate;
            CurrentTrack = trackSettings;
            AmplitudeEnvelope = new ADSR();
            PitchLFO = new SynthControlLFO();
        }


        public void NoteOn(PlaybackSettings playbackSettings)
        {
            if (AudioSettings.dspTime > playbackSettings.playTime)
            {
                return;
            }

            IsPlaying = true;
            _playbackQueue.Add(playbackSettings);
        }


        public virtual float GetDurationToEnd()
        {
            if (_playbackQueue.Count == 0) return 0;
            return (float)_playbackQueue[^1].stopTime;
        }

        protected virtual void StartPlay(PlaybackSettings playbackSettings)
        {
            IsPlaying = true;
            CurrentPlaybackSettings = playbackSettings;
            CurrentPitch = 1;
            SetPitchLFO(CurrentTrack.pitchLFOSettings);
            SetEnvelope(CurrentTrack.trackEnvelope);
            AmplitudeEnvelope.Reset();
            AmplitudeEnvelope.SetGate(true);
            if (CurrentTrack.pitchLFOSettings is { enabled: true, retrigger: true }) PitchLFO.NoteOn();
        }

        protected void HandleQueue()
        {
            while (_playbackQueue.Count > 0 && AudioSettings.dspTime >= _playbackQueue[0].playTime)
            {
                StartPlay(_playbackQueue[0]);
                _playbackQueue.RemoveAt(0);
            }

            if (AudioSettings.dspTime >= CurrentPlaybackSettings.stopTime)
            {
                AmplitudeEnvelope.SetGate(false);
            }
        }

        public abstract float[] UpdateDSP(int bufferSize, int channels);

        protected virtual void SetReady()
        {
            AmplitudeEnvelope.SetGate(false);
            IsPlaying = false;
        }

        protected void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            AmplitudeEnvelope.SetAttackRate(envelopeSettings.attack * AnywhenRuntime.SampleRate);
            AmplitudeEnvelope.SetDecayRate(envelopeSettings.decay * AnywhenRuntime.SampleRate);
            AmplitudeEnvelope.SetReleaseRate(envelopeSettings.release * AnywhenRuntime.SampleRate);
            AmplitudeEnvelope.SetSustainLevel(envelopeSettings.sustain);
            AmplitudeEnvelope.Reset();
            AmplitudeEnvelope.SetTargetRatioA(0.3f);
            AmplitudeEnvelope.SetTargetRatioDR(0.3f);
        }

        protected void SetPitchLFO(AnywhenSampleInstrument.PitchLFOSettings pitchLFOSettings)
        {
            PitchLFO.UpdateSettings(pitchLFOSettings);
        }
    }
}