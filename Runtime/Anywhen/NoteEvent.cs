using System;
using Anywhen.SettingsObjects;


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

    public int[] notes;
    public double[] chordStrum;
    public float expression1;
    public float expression2;
    public float velocity;
    public float duration;
    public AnywhenSampleInstrument.EnvelopeSettings envelope;


    public NoteEvent(EventTypes state)
    {
        this.state = state;
        notes = new[] { -1000 };
        drift = 0;
        chordStrum = new double[] { 0 };
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        duration = -1;
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }

    public NoteEvent(int note)
    {
        this.state = EventTypes.NoteOn;
        notes = new[] { note };
        drift = 0;
        chordStrum = new double[] { 0 };
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        duration = -1;
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }

    public NoteEvent(int note, float duration)
    {
        this.state = EventTypes.NoteOn;
        notes = new[] { note };
        drift = 0;
        chordStrum = new double[] { 0 };
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        this.duration = duration;
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }

    public NoteEvent(int note, float duration, AnywhenSampleInstrument.EnvelopeSettings envelope)
    {
        this.state = EventTypes.NoteOn;
        notes = new[] { note };
        drift = 0;
        chordStrum = new double[] { 0 };
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        this.duration = duration;
        this. envelope = envelope;
    }


    public NoteEvent(int[] notes)
    {
        this.state = EventTypes.NoteOn;
        this.notes = notes;
        drift = 0;
        chordStrum = new double[notes.Length];
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        duration = -1;
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }

    public NoteEvent(int note, EventTypes state)
    {
        this.state = state;
        notes = new[] { note };
        drift = 0;
        chordStrum = new double[] { 0 };
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        duration = -1;
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
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
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
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
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
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
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }


    public NoteEvent(int[] notes, EventTypes state, float velocity, double drift, double[] chordStrum,
        float expression1, float expression2)
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
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }

    public NoteEvent(int[] notes, EventTypes state, float velocity, double drift, double[] chordStrum,
        float expression1, float expression2, AnywhenSampleInstrument.EnvelopeSettings envelope)
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
        this.envelope = envelope;
    }

    [Obsolete("Obsolete")]
    public NoteEvent(EventTypes state, double drift, int[] notes, double[] chordStrum, float expression1,
        float expression2, float velocity)
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
        envelope = new AnywhenSampleInstrument.EnvelopeSettings(0, 0, 1, 0.1f);
    }
}