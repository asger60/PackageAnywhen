using System;
using Anywhen;
using Anywhen.PerformerObjects;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Samples.Scripts
{
    public class SamplePatternPlayer : MonoBehaviour
    {
        [Serializable]
        public class PatternTrack
        {
            public AnywhenInstrument anywhenInstrument;
            public PerformerObjectBase anywhenPerformerSettings;
            private int _stepIndex;
            private int _noteIndex;
            public bool[] steps;
            public AnywhenMetronome.TickRate tickRate;
            private int _prevStep = -1;
            public void OnTick()
            {
                //Debug.Log(AnywhenMetronome.Instance.Sub32);

                var stepIndex =
                    (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), steps.Length);
                if (stepIndex != _prevStep)
                {
                    _prevStep = stepIndex;
                    if (steps[stepIndex])
                    {
                        var e = anywhenPerformerSettings.MakeNote(_noteIndex, anywhenInstrument);
                        //print(e.notes.Length);
                        AnywhenRuntime.EventFunnel.HandleNoteEvent(e, anywhenInstrument, tickRate);
                        _noteIndex++;
                    }
                }
                
                
            }
        }

        public PatternTrack[] patternTracks;

        private void Start()
        {
            foreach (var track in patternTracks)
            {
                AnywhenMetronome.Instance.OnTick32 += track.OnTick;
            }
        }
    }
}