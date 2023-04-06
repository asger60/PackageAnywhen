using System;
using Anywhen;
using Anywhen.PerformerObjects;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Samples.Scripts
{
    public class PatternMixer : MonoBehaviour
    {
        [Serializable]
        public struct Pattern
        {
            public float currentWeight;
            public PerformerObjectBase performerObject;
            public bool[] patternSteps;

            public void OnTick(AnywhenInstrument instrument,
                AnywhenMetronome.TickRate tickRate)
            {
                if (currentWeight < 0.5f) return;
                int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate),
                    patternSteps.Length);
                if (patternSteps[stepIndex])
                {
                    var n = performerObject.MakeNote(stepIndex, instrument);
                    EventFunnel.HandleNoteEvent(n, instrument, tickRate);
                }
            }
        }

        public Pattern[] patterns;
        public AnywhenMetronome.TickRate tickRate;
        public AnywhenInstrument instrument;

        private void Start()
        {
            AnywhenMetronome.Instance.OnTick32 += OnTick32;
        }

        private void OnTick32()
        {
            foreach (var pattern in patterns)
            {
                pattern.OnTick( instrument, tickRate);
            }
        }
    }
}