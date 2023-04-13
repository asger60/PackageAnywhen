using System;
using Anywhen;
using UnityEngine;

namespace PackageAnywhen.Runtime.Anywhen
{
    [Serializable]
    public class StepPattern
    {
        [Serializable]
        public struct PatternStepEntry
        {
            public bool noteOn;
            public bool accent;
            public int note;
            [Range(-1f, 1f)] public float nudge;

            public float stepWeight;
        }

        public PatternStepEntry[] steps;
        //public AnywhenInstrument instrument;

        public NoteEvent OnTick(AnywhenMetronome.TickRate tickRate, float currentWeight, float swing, float humanize)
        {
            int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);

            if (steps[stepIndex].noteOn)
            {
                if (steps[stepIndex].stepWeight > currentWeight) return default;

                NoteEvent note = new NoteEvent(steps[stepIndex].note, NoteEvent.EventTypes.NoteOn, steps[stepIndex].accent ? 1 : 0.5f,
                    AnywhenMetronome.GetTiming(tickRate, swing, humanize));
                return note;
            }


            return default;
        }
    }
}