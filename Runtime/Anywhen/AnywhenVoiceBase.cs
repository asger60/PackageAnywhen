using Anywhen.Composing;

namespace Anywhen
{
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady { get; protected set; }
        public bool HasScheduledPlay => _hasScheduledPlay;

        internal bool _hasScheduledPlay;
        protected bool _isPlaying;
        public bool IsPlaying => _isPlaying;

        public struct PlaybackSettings
        {
            public double PlayTime;
            public double StopTime;
            public float Volume;
            public float Pitch;
            public int Note;

            public PlaybackSettings(double playTime, double stopTime, float volume, float pitch, int note)
            {
                PlayTime = playTime;
                StopTime = stopTime;
                Volume = volume;
                Pitch = pitch;
                Note = note;
            }
        }

        protected PlaybackSettings CurrentPlaybackSettings;
        protected PlaybackSettings NextPlaybackSettings;

        public abstract void NoteOn(PlaybackSettings playbackSettings);
        
        public abstract void Init(int sampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings);

        public abstract float GetDurationToEnd();

        public abstract float[] UpdateDSP(int bufferSize, int channels);
    }
}