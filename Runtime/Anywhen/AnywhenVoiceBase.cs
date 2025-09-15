using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenVoiceBase
    {
        public bool IsReady { get; protected set; }
        public bool HasScheduledPlay => _hasScheduledPlay;

        internal bool _hasScheduledPlay;
        public virtual void NoteOn(int note, double playTime, double duration, float volume, AnywhenInstrument instrument, AnywhenSampleInstrument.EnvelopeSettings envelope)
        {
            
        }
        public virtual void Init(int sampleRate) {}

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