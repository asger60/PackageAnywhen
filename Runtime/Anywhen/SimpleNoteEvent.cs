using System;

public struct SimpleNoteEvent : IEquatable<SimpleNoteEvent>
{
    public int note;
    public double drift;
    public float velocity;
    public float duration;
    public float chance;

    
   
    public SimpleNoteEvent(int note)
    {
        this.note = note;
        drift = 0;
        velocity = 1;
        duration = 0.025f;
        chance = 1;
    }

    public bool Equals(SimpleNoteEvent other)
    {
        return note == other.note && drift.Equals(other.drift) && velocity.Equals(other.velocity) &&
               duration.Equals(other.duration) && chance.Equals(other.chance);
    }

    public override bool Equals(object obj)
    {
        return obj is SimpleNoteEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(note, drift, velocity, duration, chance);
    }
}