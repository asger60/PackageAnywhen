using UnityEngine;

namespace Anywhen
{
    public class AnywhenVoiceBase
    {
        //public virtual void NoteOn(int[] notes, double playTime, double duration, AnywhenInstrument instrument)
        //{
        //    
        //}

        public virtual float[] UpdateDSP(int bufferSize, int channels)
        {
            return new float[bufferSize];
        }
    }
}