using System;

namespace Anywhen
{
    [Serializable]
    public struct NoteEvent
    {
        public enum EventTypes
        {
            NoteOn,
            NoteOff,
            NoteDown,
        }

        public EventTypes state;
        public double drift;
        //public AnywhenMetronome.TickRate quantization;
        public int[] notes;
        public double[] chordStrum;
        public float expression1;
        public float expression2;
        public float velocity;
        public float duration;

       

        public NoteEvent(int note, EventTypes state)
        {
            this.state = state;
            notes = new[] { note };
            this.state = state;
            drift = 0;
            chordStrum = new double[] { 0 };
            expression1 = 0;
            expression2 = 0;
            velocity = 1;
            duration = -1;
        }
        
        public NoteEvent(int note, float noteDuration, EventTypes state)
        {
            this.state = state;
            notes = new[] { note };
            this.state = state;
            drift = 0;
            chordStrum = new double[] { 0 };
            expression1 = 0;
            expression2 = 0;
            velocity = 1;
            duration = noteDuration;
        }
    
        public NoteEvent(int note, EventTypes state, float volume)
        {
            this.state = state;
            notes = new[] { note };
            this.state = state;
            drift = 0;
            chordStrum = new double[] { 0 };
            expression1 = 0;
            expression2 = 0;
            velocity = volume;
            duration = -1;
        }
    
        public NoteEvent(int note, EventTypes state, float volume, double drift)
        {
            this.state = state;
            notes = new[] { note };
            this.state = state;
            this.drift = drift;
            chordStrum = new double[] { 0 };
            expression1 = 0;
            expression2 = 0;
            velocity = volume;
            duration = -1;
        }



        public NoteEvent(EventTypes state, double drift, int[] notes, double[] chordStrum, float expression1, float expression2, float velocity)
        {
            this.state = state;
            this.drift = drift;
            this.notes = notes;
            this.chordStrum = chordStrum;
            this.expression1 = expression1;
            this.expression2 = expression2;
            this.velocity = 1;
            this.velocity = velocity;
            duration = -1;
        }

    }
}