using System;
using Anywhen.SettingsObjects;
using UnityEngine;

[Serializable]
public class StepPattern
{
    [Serializable]
    public struct PatternStepEntry
    {
        public int index;
        public bool noteOn;
        public bool accent;
        [Range(-1f, 1f)]
        public float nudge;
    }

    public PatternStepEntry[] steps;
    public AnywhenInstrument instrument;
    
}