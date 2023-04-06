using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.PerformerObjects
{
    [CreateAssetMenu(fileName = "New Percussion Performer", menuName = "Anywhen/Performers/Percussion", order = 51)]
    public class PerformerObjectPercussion : PerformerObjectBase
    {
        public override NoteEvent MakeNote(int sequenceStep, AnywhenInstrument instrument)
        {
            if (instrument == null)
            {
                return default;
            }

            noteOnEvent = new NoteEvent(0, NoteEvent.EventTypes.NoteOn, GetVolume(), GetTiming());
            return noteOnEvent;
            //EventFunnel.HandleNoteEvent(noteOnEvent, instrument);
        }
    }
}