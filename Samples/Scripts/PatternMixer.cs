using System;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class PatternMixer : MonoBehaviour
    {
        public PatternCollection savePatternCollection;
        [Range(0, 3f)] public float currentPatternMix;
        [Range(0, 3f)] public float currentInstrumentMix;


        public AnimationCurve mixCurve;

        [Serializable]
        public struct Pattern
        {
            [Range(0, 1f)] public float currentWeight;
            public StepPattern[] patternTracks;
            [Range(0, 1f)] public float swing;
            [Range(0, 1f)] public float humanize;

            public NoteEvent OnTick(AnywhenMetronome.TickRate tickRate)
            {
                int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);

                for (var i = 0; i < patternTracks.Length; i++)
                {
                    var patternTrack = patternTracks[i];
                    if (patternTrack.steps[stepIndex].noteOn)
                    {
                        if (patternTrack.steps[stepIndex].stepWeight > currentWeight) return default;

                        NoteEvent note = new NoteEvent(0, NoteEvent.EventTypes.NoteOn,
                            patternTrack.steps[stepIndex].accent ? 1 : 0.5f,
                            AnywhenMetronome.GetTiming(tickRate, swing, humanize));
                        return note;
                    }
                }

                return default;
            }

            public bool ShouldTrigger(int trackIndex, int stepIndex)
            {
                if (currentWeight <= 0) return false;

                if (patternTracks.Length <= trackIndex) return false;

                return patternTracks[trackIndex].steps[stepIndex].noteOn &&
                       (currentWeight > patternTracks[trackIndex].steps[stepIndex].stepWeight);
            }
        }

        [Serializable]
        public struct PatternInstrument
        {
            public InstrumentObject[] instruments;
            public float currentWeight;
            public StepPattern[] patternTracks;

            public float GetStepWeight(int trackIndex, int stepIndex)
            {
                if (currentWeight <= 0) return 0;
                return patternTracks[trackIndex].steps[stepIndex].stepWeight;
            }
        }

        [Serializable]
        public struct InstrumentObject
        {
            public AnywhenInstrument instrument;
            public Color color;
        }


        public PatternInstrument[] patternInstruments;

        public Pattern[] patterns;
        public AnywhenMetronome.TickRate tickRate;
        public Slider uiSliderPattern;
        public Slider uiSliderInstrument;


        private void Start()
        {
            AnywhenMetronome.Instance.OnTick16 += OnTick;
        }

        private void OnTick()
        {
            int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);

            for (var i = 0; i < patterns.Length; i++)
            {
                for (var i1 = 0; i1 < patterns[i].patternTracks.Length; i1++)
                {
                    var n = patterns[i].patternTracks[i1].OnTick(tickRate, patterns[i].currentWeight, 0, 0);
                    if (n.notes != null)
                    {
                        EventFunnel.HandleNoteEvent(n, GetInstrumentForTrack(stepIndex, i1).instrument, tickRate);
                    }
                }
            }
        }

        InstrumentObject GetInstrumentForTrack(int stepIndex, int trackIndex)
        {
            float bestStepWeight = 0;
            InstrumentObject instrumentObject = patternInstruments[0].instruments[0];
            foreach (var patternInstrument in patternInstruments)
            {
                float thisStepWeight = patternInstrument.currentWeight +
                                       patternInstrument.GetStepWeight(trackIndex, stepIndex);
                if (thisStepWeight > bestStepWeight)
                {
                    bestStepWeight = thisStepWeight;
                    instrumentObject = patternInstrument.instruments[trackIndex];
                }
            }

            return instrumentObject;
        }

        private void Update()
        {
            currentPatternMix = uiSliderPattern.value;
            currentInstrumentMix = uiSliderInstrument.value;
            for (int i = 0; i < patterns.Length; i++)
            {
                patterns[i].currentWeight = Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentPatternMix)));
            }

            for (var i = 0; i < patternInstruments.Length; i++)
            {
                patternInstruments[i].currentWeight =
                    Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentInstrumentMix)));
            }
        }

        private readonly bool[] _currentPattern = new bool[16];

        public bool[] GetCurrentPattern(int trackIndex)
        {
            for (int i = 0; i < 16; i++)
            {
                _currentPattern[i] = false;
            }


            foreach (var pattern in patterns)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (pattern.ShouldTrigger(trackIndex, i))
                        _currentPattern[i] = true;
                }
            }


            return _currentPattern;
        }

        private readonly InstrumentObject[] _currentInstruments = new InstrumentObject[16];

        public InstrumentObject[] GetInstruments(int trackIndex)
        {
            for (int i = 0; i < 16; i++)
            {
                _currentInstruments[i] = GetInstrumentForTrack(i, trackIndex);
            }

            return _currentInstruments;
        }

#if UNITY_EDITOR
        [ContextMenu("SavePattern")]
        void SavePattern()
        {
            savePatternCollection.patterns = patterns;
            EditorUtility.SetDirty(savePatternCollection);
        }

        [ContextMenu("LoadPattern")]
        void LoadPattern()
        {
            patterns = savePatternCollection.patterns;
        }

        [ContextMenu("Randomize step weights")]
        void RandomizeStepWeights()
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                foreach (var track in patterns[i].patternTracks)
                {
                    for (var index = 0; index < track.steps.Length; index++)
                    {
                        track.steps[index].stepWeight = Random.Range(0, 1f);
                    }
                }
            }
        }

        [ContextMenu("Randomize instrument weights")]
        void RandomizeInstrumentWeights()
        {
            for (int i = 0; i < patternInstruments.Length; i++)
            {
                foreach (var track in patternInstruments[i].patternTracks)
                {
                    for (var index = 0; index < track.steps.Length; index++)
                    {
                        track.steps[index].stepWeight = Random.Range(0, 1f);
                    }
                }
            }
        }
#endif
    }
}