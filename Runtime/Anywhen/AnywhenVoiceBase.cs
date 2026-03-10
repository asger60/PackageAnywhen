using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady => _playbackQueue.Count == 0 && !IsPlaying;
        protected AnysongTrack CurrentTrack;
        protected ADSR AmplitudeEnvelope = new();
        protected bool IsPlaying;
        public bool HasScheduledPlay => _playbackQueue.Count > 0;
        private readonly List<PlaybackSettings> _playbackQueue = new List<PlaybackSettings>();
        protected SynthControlLFO PitchLFO;
        protected double CurrentPitch;
        protected float CurrentSampleRate;
        

        public struct PlaybackSettings
        {
            public double PlayTime;
            public double StopTime;
            public float Volume;
            public int Note;

            public PlaybackSettings(double playTime, double stopTime, float volume, int note)
            {
                PlayTime = playTime;
                StopTime = stopTime;
                Volume = volume;
                Note = note;
            }
        }

        protected PlaybackSettings CurrentPlaybackSettings;

        
        
        public void NoteOn(PlaybackSettings playbackSettings)
        {
            if (AudioSettings.dspTime > playbackSettings.PlayTime) return;
            IsPlaying = true;
            _playbackQueue.Add(playbackSettings);
        }

        protected AnywhenVoiceBase(AnywhenInstrument instrumentSettings, AnysongTrack trackSettings)
        {
            CurrentSampleRate = AudioSettings.outputSampleRate;
            CurrentTrack = trackSettings;
            AmplitudeEnvelope = new ADSR();
            PitchLFO = new SynthControlLFO();
        }



        public virtual float GetDurationToEnd()
        {
            if (_playbackQueue.Count == 0) return 0;
            return (float)_playbackQueue[^1].StopTime;
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
            Debug.Log("handle queue");
            while (_playbackQueue.Count > 0 && AudioSettings.dspTime >= _playbackQueue[0].PlayTime)
            {
                StartPlay(_playbackQueue[0]);
                _playbackQueue.RemoveAt(0);
            }

            if (AudioSettings.dspTime >= CurrentPlaybackSettings.StopTime)
            {
                AmplitudeEnvelope.SetGate(false);
            }
        }

        public abstract float[] UpdateDSP(int bufferSize, int channels);

        protected void SetReady()
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