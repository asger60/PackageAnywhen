using Anywhen.Composing;
using Anywhen.SettingsObjects;

namespace Anywhen
{
    public class AnywhenVoiceBase
    {
        public bool IsReady { get; protected set; }
        public bool HasScheduledPlay => _hasScheduledPlay;

        internal bool _hasScheduledPlay;
        protected bool _isPlaying;
        public bool IsPlaying => _isPlaying;

        protected struct PlaybackSettings
        {
            public double PlayTime;
            public double StopTime;
            public float Volume;
            public float Pitch;
            public int Note;
            public AnywhenNoteClip NoteClip;

            public PlaybackSettings(double playTime, double stopTime, float volume, float pitch, int note,
                AnywhenNoteClip noteClip = null)
            {
                PlayTime = playTime;
                StopTime = stopTime;
                Volume = volume;
                Pitch = pitch;
                Note = note;
                NoteClip = noteClip;
            }
        }

        protected PlaybackSettings _currentPlaybackSettings;
        protected PlaybackSettings _nextPlaybackSettings;

        public virtual void NoteOn(int note, double playTime, double duration, float volume)
        {
        }


        public virtual void Init(int sampleRate, AnywhenInstrument instrument, AnysongTrack track)
        {
        }

        public virtual float GetDurationToEnd()
        {
            return 0;
        }

        public virtual float[] UpdateDSP(int bufferSize, int channels)
        {
            return new float[bufferSize];
        }
    }
}