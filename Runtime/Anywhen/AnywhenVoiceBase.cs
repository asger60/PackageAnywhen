using System.Collections.Generic;
using Anywhen.Composing;

namespace Anywhen
{
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady { get; protected set; }

        protected bool isPlaying;
        public bool IsPlaying => isPlaying;
        public bool HasScheduledPlay => playbackQueue.Count > 0;
        protected readonly List<PlaybackSettings> playbackQueue = new List<PlaybackSettings>();

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
    }
}