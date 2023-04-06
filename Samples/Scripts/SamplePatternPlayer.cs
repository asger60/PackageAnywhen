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

            public void OnTick()
            {
                //Debug.Log(AnywhenMetronome.Instance.Sub32);
                if (AnywhenMetronome.Instance.Sub32 % (32 / (int)anywhenPerformerSettings.playbackRate) != 0) return;
                _stepIndex++;
                _stepIndex = (int)Mathf.Repeat(_stepIndex, steps.Length);
                if (steps[_stepIndex])
                {
                    anywhenPerformerSettings.Play(_noteIndex, anywhenInstrument);
                    _noteIndex++;
                }
            }
        }

        public PatternTrack[] patternTracks;

        private void Start()
        {
            foreach (var track in patternTracks)
            {
                AnywhenMetronome.Instance.OnTick32 += track.OnTick;
                //switch (track.anywhenPerformerSettings.playbackRate)
                //{
                //    case AnywhenMetronome.TickRate.None:
                //        break;
                //    case AnywhenMetronome.TickRate.Sub2:
                //        AnywhenMetronome.Instance.OnTick2 += track.OnTick;
                //        break;
                //    case AnywhenMetronome.TickRate.Sub4:
                //        AnywhenMetronome.Instance.OnTick4 += track.OnTick;
                //        break;
                //    case AnywhenMetronome.TickRate.Sub8:
                //        AnywhenMetronome.Instance.OnTick8 += track.OnTick;
                //        break;
                //    case AnywhenMetronome.TickRate.Sub16:
                //        AnywhenMetronome.Instance.OnTick16 += track.OnTick;
                //        break;
                //    case AnywhenMetronome.TickRate.Sub32:
                //        AnywhenMetronome.Instance.OnTick32 += track.OnTick;
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
            }
        }
    }
}