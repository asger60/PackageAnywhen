using System;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace PackageAnywhen.Runtime.Anywhen
{
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

            public float stepWeight;
        }

        public PatternStepEntry[] steps;
        public AnywhenInstrument instrument;
    
    }
}