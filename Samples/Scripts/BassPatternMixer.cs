using System;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class BassPatternMixer : MonoBehaviour, IMixableObject
    {
        public PatternCollection savePatternCollection;
        [Range(0, 3f)] public float currentPatternMix;

        [FormerlySerializedAs("currentInstrumentMix")] [Range(0, 3f)]
        public float currentMelodyMix;

        public int patternLength;
        public AnimationCurve mixCurve;

        [Serializable]
        public struct BassPattern
        {
            [Range(0, 1f)] public float currentWeight;
            public StepPattern pattern;

            public bool ShouldTrigger(int stepIndex)
            {
                if (currentWeight <= 0) return false;
                return pattern.steps[stepIndex].noteOn && (currentWeight > pattern.steps[stepIndex].stepWeight);
            }
        }


        [Serializable]
        public struct InstrumentObject
        {
            public AnywhenInstrument instrument;
            public Color color;
        }


        public DrumPatternMixer.PatternInstrument[] patternInstruments;

        [FormerlySerializedAs("patterns")] public BassPattern[] triggerPatterns;
        public BassPattern[] melodyPatterns;
        public AnywhenMetronome.TickRate tickRate;
        private BassPatternVisualizer _bassPatternVisualizer;
        [SerializeField] private MixView mixView;
        private readonly int[] _goodNotes = new[] { 1, 2, 4, 6, -1, -2 };

        private readonly bool[] _currentPattern = new bool[32];
        private readonly int[] _currentNotePattern = new int[32];
        private int _barsCounter;


        private void Start()
        {
            TrackHandler.Instance.AttachMixInterface(this, 1);
            AnywhenMetronome.Instance.OnTick16 += OnTick;
            _bassPatternVisualizer = GetComponent<BassPatternVisualizer>();
        }


        private void OnTick()
        {
            int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);
            if (stepIndex == 0)
                _barsCounter++;

            _barsCounter = (int)Mathf.Repeat(_barsCounter, 2);
            int currentStep = stepIndex + (_barsCounter * 16);


            if (GetCurrentPattern(0)[currentStep])
            {
                var n = GetNoteForStep(currentStep);
                NoteEvent note = new NoteEvent(n, NoteEvent.EventTypes.NoteOn, 1,
                    AnywhenMetronome.GetTiming(tickRate, 0, 0));

                EventFunnel.HandleNoteEvent(note, patternInstruments[0].instruments[0].instrument, tickRate);
            }
        }


        private void Update()
        {
            //if (uiSliderPattern)
            //    currentPatternMix = uiSliderPattern.value;
            //if (uiSliderInstrument)
            //    currentMelodyMix = uiSliderInstrument.value;
            //for (int i = 0; i < triggerPatterns.Length; i++)
            //{
            //    triggerPatterns[i].currentWeight =
            //        Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentPatternMix)));
            //}
//
            //for (var i = 0; i < melodyPatterns.Length; i++)
            //{
            //    melodyPatterns[i].currentWeight =
            //        Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentMelodyMix)));
            //}
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


        public bool[] GetCurrentPattern(int trackIndex)
        {
            for (int i = 0; i < 32; i++)
            {
                _currentPattern[i] = false;
            }


            foreach (var pattern in triggerPatterns)
            {
                for (int i = 0; i < 32; i++)
                {
                    if (pattern.ShouldTrigger(i))
                        _currentPattern[i] = true;
                }
            }


            return _currentPattern;
        }


        public int[] GetCurrentNotePattern(int trackIndex)
        {
            //foreach (var pattern in melodyPatterns)
            {
                for (int i = 0; i < 32; i++)
                {
                    _currentNotePattern[i] = GetNoteForStep(i);
                }
            }


            return _currentNotePattern;
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
                            melodyPatterns[i].pattern.steps[index1].note = GetRandomNote(1);
                            break;
                        case 1:
                            melodyPatterns[i].pattern.steps[index1].note = GetRandomNote(2);
                            break;
                        case 2:
                            melodyPatterns[i].pattern.steps[index1].note = GetRandomNote(5);
                            break;
                        case 3:
                            melodyPatterns[i].pattern.steps[index1].note = GetRandomNote(7);
                            break;
                        case 4:


                            break;
                    }


                    melodyPatterns[i].pattern.steps[index1].stepWeight = Random.Range(0, 1f);
                }
            }
        }


        int GetRandomNote(int width)
        {
            int rnd = Random.Range(0, 5);
            if (rnd == 0) return 0;

            width = Mathf.Min(width, _goodNotes.Length);
            return _goodNotes[Random.Range(0, width)];
        }

#endif


        public void Mix(int patternIndex)
        {
            float combinedWeight = 0;
            triggerPatterns[patternIndex].currentWeight += 0.05f;
            float[] values = new float[4];
            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                combinedWeight += triggerPatterns[i].currentWeight;
            }

            if (combinedWeight > 1)
            {
                float subtract = (combinedWeight - 1) / 3f;
                for (int i = 0; i < triggerPatterns.Length; i++)
                {
                    if (i != patternIndex)
                        triggerPatterns[i].currentWeight -= subtract;
                }
            }

            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                triggerPatterns[i].currentWeight = Mathf.Clamp01(triggerPatterns[i].currentWeight);
                melodyPatterns[i].currentWeight = triggerPatterns[i].currentWeight; 
                //patternInstruments[i].currentWeight = triggerPatterns[i].currentWeight;
                values[i] = triggerPatterns[i].currentWeight;
            }

            mixView.UpdateValues(values);
        }

        public void SetIsActive(bool state)
        {
            _bassPatternVisualizer.SetIsTrackActive(state);
        }
    }
}