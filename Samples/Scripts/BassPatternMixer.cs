using System;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class BassPatternMixer : MonoBehaviour, IMixableObject
    {
        public PatternCollection savePatternCollection;

        public AnywhenInstrument instrument;

        [Serializable]
        public struct BassPattern
        {
            [Range(0, 1f)] public float currentWeight;
            public StepPattern pattern;

            public bool ShouldTrigger(int stepIndex)
            {
                //if (currentWeight <= 0) return false;
                return pattern.steps[stepIndex].noteOn && (0.5f > pattern.steps[stepIndex].stepWeight);
            }
        }


        [FormerlySerializedAs("patterns")] public BassPattern[] triggerPatterns;
        public BassPattern[] melodyPatterns;
        public AnywhenMetronome.TickRate tickRate;
        private BassPatternVisualizer _bassPatternVisualizer;
        [SerializeField] private MixView mixView;
        private readonly int[] _goodNotes = new[] { 1, 2, 4, 6, -1, -2 };

        //private readonly bool[] _currentPattern = new bool[32];
        //private readonly int[] _currentNotePattern = new int[32];
        private int _barsCounter;
        
        public bool[] CurrentTriggerPattern => _currentTriggerPattern;
        public int[] CurrentNotePattern => _currentNotePattern;
        private bool[] _currentTriggerPattern = new bool[32];
        private int[][] _currentChordPattern = new int[32][];
        private int[] _currentNotePattern = new int[32];
        
        float[] _currentTriggerMixValue = new float[32];
        float[] _currentNoteMixValue = new float[32];

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


            if (CurrentTriggerPattern[currentStep])
            {
                var n = GetNoteForStep(currentStep);
                NoteEvent note = new NoteEvent(n, NoteEvent.EventTypes.NoteOn, 1,
                    AnywhenMetronome.GetTiming(tickRate, 0, 0));

                EventFunnel.HandleNoteEvent(note, instrument, tickRate);
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


        //public bool[] GetCurrentPattern(int trackIndex)
        //{
        //    for (int i = 0; i < 32; i++)
        //    {
        //        _currentPattern[i] = false;
        //    }
//
//
        //    foreach (var pattern in triggerPatterns)
        //    {
        //        for (int i = 0; i < 32; i++)
        //        {
        //            if (pattern.ShouldTrigger(i))
        //                _currentPattern[i] = true;
        //        }
        //    }
//
//
        //    return _currentPattern;
        //}


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

        private void Update()
        {
            GetCurrentTriggerPattern();
        }

        private void GetCurrentTriggerPattern()
        {
            for (int i = 0; i < 32; i++)
            {
                float weightDistance = 100;
                for (var index = 0; index < triggerPatterns.Length; index++)
                {
                    float thisWeightDistance =
                        (Mathf.Abs(_currentTriggerMixValue[i] - index) -
                         (triggerPatterns[index].pattern.steps[i].stepWeight));

                    if (thisWeightDistance < weightDistance)
                    {
                        weightDistance = thisWeightDistance;
                        _currentTriggerPattern[i] = triggerPatterns[index].pattern.steps[i].noteOn;
                    }
                }
            }
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
                for (var index = 0; index < triggerPatterns[i].pattern.steps.Length; index++)
                {
                    triggerPatterns[i].pattern.steps[index].stepWeight = Random.Range(0f, 0.9f);
                }
            }

            for (int i = 0; i < melodyPatterns.Length; i++)
            {
                for (var index = 0; index < melodyPatterns[i].pattern.steps.Length; index++)
                {
                    melodyPatterns[i].pattern.steps[index].stepWeight = Random.Range(0f, 0.8f);
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

        void MixTriggers(int mixIndex, int stepIndex)
        {
            
            float add = mixIndex == 1 ? -0.25f : 0.25f;
            int range = 3;
            for (int i = stepIndex - range; i < stepIndex + range; i++)
            {
                int paintIndex = (int)Mathf.Repeat(i, 32);
                float effect = Mathf.InverseLerp(range, 0f, Mathf.Abs(stepIndex - paintIndex));
                _currentTriggerMixValue[paintIndex] += add * effect;
                _currentTriggerMixValue[paintIndex] =
                    Mathf.Clamp(_currentTriggerMixValue[paintIndex], 0, triggerPatterns.Length);
            }
            
            
            
            //float combinedWeight = 0;
//
//
//
            //int patternIndex = mixIndex;
            //int range = 3;
//
            //for (int i = 0; i < triggerPatterns.Length; i++)
            //{
            //    for (int step = 0; step < triggerPatterns[i].pattern.steps.Length; step++)
            //    {
            //        var b = Mathf.Abs(step - stepIndex) < range;
//
            //        if (b)
            //        {
            //            float add = patternIndex != i ? Random.Range(-.1f, -.05f) : Random.Range(.1f, .05f);
//
            //            triggerPatterns[i].pattern.steps[step].stepWeight += add;
            //            triggerPatterns[i].pattern.steps[step].stepWeight =
            //                Mathf.Clamp01(triggerPatterns[i].pattern.steps[step].stepWeight);
            //        }
            //    }
            //}
        }

        void MixMelody(int mixIndex, int stepIndex)
        {
            mixIndex -= 2;


            int patternIndex = mixIndex;
            int range = 3;

            for (int i = 0; i < melodyPatterns.Length; i++)
            {
                for (int step = 0; step < melodyPatterns[i].pattern.steps.Length; step++)
                {
                    var b = Mathf.Abs(step - stepIndex) < range;

                    if (b)
                    {
                        float add = patternIndex != i ? Random.Range(-.1f, -.05f) : Random.Range(.1f, .05f);

                        melodyPatterns[i].pattern.steps[step].stepWeight += add;
                        melodyPatterns[i].pattern.steps[step].stepWeight =
                            Mathf.Clamp01(melodyPatterns[i].pattern.steps[step].stepWeight);
                    }
                }
            }
        }

        public void Mix(int patternIndex, int stepIndex)
        {
            if (patternIndex < 2)
            {
                MixTriggers(patternIndex, stepIndex);
            }
            else
            {
                MixMelody(patternIndex, stepIndex);
            }

            float[] values = new float[4];

            values[0] = triggerPatterns[0].currentWeight / 2f;
            values[1] = triggerPatterns[1].currentWeight / 2f;
            values[2] = melodyPatterns[0].currentWeight / 2f;
            values[3] = melodyPatterns[1].currentWeight / 2f;


            mixView.UpdateValues(values);
        }

        public void SetIsActive(bool state)
        {
            _bassPatternVisualizer.SetIsTrackActive(state);
        }
    }
}