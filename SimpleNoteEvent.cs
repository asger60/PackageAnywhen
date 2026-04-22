
public struct SimpleNoteEvent
{
    //public enum EventTypes
    //{
    //    NoteOn,
    //    NoteOff,
    //    NoteDown,
    //}

    //public EventTypes state;

    public int note;
    public double drift;
    public int chordStrum;
    public double expression1;
    public double expression2;
    public double velocity;
    public float duration;


    public SimpleNoteEvent(int note)
    {
        this.note = note;
        drift = 0;
        chordStrum = 0;
        expression1 = 0;
        expression2 = 0;
        velocity = 1;
        duration = 0.25f;
    }


}