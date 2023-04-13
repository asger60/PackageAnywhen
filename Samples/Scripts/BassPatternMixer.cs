using System;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class BassPatternMixer : MonoBehaviour
    {
        public PatternCollection savePatternCollection;
        [Range(0, 3f)] public float currentPatternMix;

        [FormerlySerializedAs("currentInstrumentMix")] [Range(0, 3f)]
        public float currentMelodyMix;


        public AnimationCurve mixCurve;

        [Serializable]
        public struct BassPattern
        {
            [Range(0, 1f)] public float currentWeight;
            public StepPattern pattern;

            public bool ShouldTrigger(int stepIndex)
            {
                if (currentWeight <= 0) return false;

                //if (patternTracks.Length <= trackIndex) return false;

                return pattern.steps[stepIndex].noteOn && (currentWeight > pattern.steps[stepIndex].stepWeight);
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

        [FormerlySerializedAs("patterns")] public BassPattern[] triggerPatterns;
        public BassPattern[] melodyPatterns;
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

            for (var i = 0; i < triggerPatterns.Length; i++)
            {
                //for (var i1 = 0; i1 < patterns[i].patternTracks.Length; i1++)
                {
                    var n = triggerPatterns[i].pattern.OnTick(tickRate, triggerPatterns[i].currentWeight, 0, 0);
                    if (n.notes != null)
                    {
                        n.notes[0] = GetNoteForStep(stepIndex);

                        EventFunnel.HandleNoteEvent(n, GetInstrumentForTrack(stepIndex, 0).instrument, tickRate);
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
            if (uiSliderPattern)
                currentPatternMix = uiSliderPattern.value;
            if (uiSliderInstrument)
                currentMelodyMix = uiSliderInstrument.value;
            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                triggerPatterns[i].currentWeight =
                    Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentPatternMix)));
            }

            for (var i = 0; i < melodyPatterns.Length; i++)
            {
                melodyPatterns[i].currentWeight =
                    Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentMelodyMix)));
            }
        }

        int GetNoteForStep(int stepIndex)
        {
            int note = 0;
            float bestWeight = 0;
            foreach (var melodyPattern in melodyPatterns)
            {
                float thisWeight = melodyPattern.currentWeight * melodyPattern.pattern.steps[stepIndex].stepWeight;
                if (thisWeight > bestWeight)
                {
                    note = melodyPattern.pattern.steps[stepIndex].note;
                    bestWeight = thisWeight;
                }
            }

            return note;
        }

        private readonly bool[] _currentPattern = new bool[16];

        public bool[] GetCurrentPattern(int trackIndex)
        {
            for (int i = 0; i < 16; i++)
            {
                _currentPattern[i] = false;
            }


            foreach (var pattern in triggerPatterns)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (pattern.ShouldTrigger(i))
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
            //savePatternCollection.patterns = patterns;
            EditorUtility.SetDirty(savePatternCollection);
        }

        [ContextMenu("LoadPattern")]
        void LoadPattern()
        {
            //patterns = savePatternCollection.patterns;
        }

        [ContextMenu("Randomize step weights")]
        void RandomizeStepWeights()
        {
            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                //foreach (var track in patterns[i].patternTracks)
                {
                    for (var index = 0; index < triggerPatterns[i].pattern.steps.Length; index++)
                    {
                        triggerPatterns[i].pattern.steps[index].stepWeight = Random.Range(0, 1f);
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

        [ContextMenu("Randomize notes")]
        void RandomizeMelodyNotes()
        {
            for (int i = 0; i < melodyPatterns.Length; i++)
            {
                for (var index1 = 0; index1 < melodyPatterns[i].pattern.steps.Length; index1++)
                {
                    int rnd = Random.Range(0, i + 1);
                    switch (rnd)
                    {
                        case 0:
                            melodyPatterns[i].pattern.steps[index1].note = 0;
                            break;
                        case 1:
                            melodyPatterns[i].pattern.steps[index1].note = 1;
                            break;
                        case 2:
                            melodyPatterns[i].pattern.steps[index1].note = 2;
                            break;
                        case 3:
                            int rnd1 = Random.Range(0, 1);
                            melodyPatterns[i].pattern.steps[index1].note = rnd1 == 0 ? 7 : 4;
                            break;
                        case 4:


                            break;
                    }


                    melodyPatterns[i].pattern.steps[index1].stepWeight = Random.Range(0, 1f);
                }
            }
        }
#endif
    }
}