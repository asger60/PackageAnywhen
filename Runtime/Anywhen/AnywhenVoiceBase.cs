using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;

namespace Anywhen
{
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady { get; protected set; }
        protected AnysongTrack _currentTrack;
        protected ADSR _adsr = new();
        protected bool isPlaying;
        public bool IsPlaying => isPlaying;
        public bool HasScheduledPlay => playbackQueue.Count > 0;
        protected readonly List<PlaybackSettings> playbackQueue = new List<PlaybackSettings>();
        protected SynthSettingsObjectLFO _pitchSettings;
        protected SynthControlLFO _pitchLFO;
        protected double _currentPitch;
        protected float _currentSampleRate;
        

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

        public abstract void NoteOn(PlaybackSettings playbackSettings);
        
        public abstract void Init(int sampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings);

        public abstract float GetDurationToEnd();

        public abstract float[] UpdateDSP(int bufferSize, int channels);
        
        protected void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * _currentSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * _currentSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * _currentSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }

        protected void SetPitchLFO(AnywhenSampleInstrument.PitchLFOSettings pitchLFOSettings)
        {
            _pitchLFO.UpdateSettings(pitchLFOSettings);
        }
    }
}