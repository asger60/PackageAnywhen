using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady => playbackQueue.Count == 0 && !isPlaying; 
        protected AnysongTrack currentTrack;
        protected ADSR adsr = new();
        protected bool isPlaying;
        public bool IsPlaying => isPlaying;
        public bool HasScheduledPlay => playbackQueue.Count > 0;
        protected readonly List<PlaybackSettings> playbackQueue = new List<PlaybackSettings>();
        protected SynthSettingsObjectLFO _pitchSettings;
        protected SynthControlLFO pitchLFO;
        protected double currentPitch;
        protected float currentSampleRate;
        

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
            SetPitchLFO(currentTrack.pitchLFOSettings);
            SetEnvelope(currentTrack.trackEnvelope);
            playbackQueue.Add(playbackSettings);
        }
        
        public abstract void Init(int sampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings);

        public abstract float GetDurationToEnd();

        public abstract float[] UpdateDSP(int bufferSize, int channels);
        
        protected void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            adsr.SetAttackRate(envelopeSettings.attack * AnywhenRuntime.SampleRate);
            adsr.SetDecayRate(envelopeSettings.decay * AnywhenRuntime.SampleRate);
            adsr.SetReleaseRate(envelopeSettings.release * AnywhenRuntime.SampleRate);
            adsr.SetSustainLevel(envelopeSettings.sustain);
            adsr.Reset();
            adsr.SetTargetRatioA(0.3f);
            adsr.SetTargetRatioDR(0.3f);
        }

        protected void SetPitchLFO(AnywhenSampleInstrument.PitchLFOSettings pitchLFOSettings)
        {
            pitchLFO.UpdateSettings(pitchLFOSettings);
        }
    }
}