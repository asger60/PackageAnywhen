using System;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrack
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        private NoteEvent _lastTrackEvent;


        public void Init()
        {
            volume = 1;
        }

        public AnysongTrack Clone()
        {
            var clone = new AnysongTrack
            {
                instrument = instrument,
                volume = volume
            };

            return clone;
        }

        public void TriggerNoteOn(AnyPatternStep anyPatternStep, AnyPattern pattern)
        {
            _lastTrackEvent = anyPatternStep.GetEvent(pattern.rootNote);
            _lastTrackEvent.velocity *= volume;
            AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackEvent, instrument);
        }



    }
}