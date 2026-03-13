using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongPattern
    {
        public List<float> triggerChances = new List<float>();

        public List<AnysongPatternStep> steps;
        [Range(0, 16)] public int patternLength = 16;
        private int _internalIndex;
        public int InternalIndex => _internalIndex;

        public void Init()
        {
            triggerChances.AddRange(new[] { 0f, 0f, 0f, 0f });

            steps = new List<AnysongPatternStep>();
            for (int i = 0; i < 16; i++)
            {
                var newStep = new AnysongPatternStep();
                newStep.Init();
                steps.Add(newStep);
            }
        }

        public AnysongPattern Clone()
        {
            var clone = new AnysongPattern
            {
                steps = new List<AnysongPatternStep>()
            };
            for (var i = 0; i < 16; i++)
            {
                clone.steps.Add(steps[i].Clone());
            }

            clone.triggerChances.AddRange(triggerChances);
            return clone;
        }

        public bool TriggerOnBar(int currentBar)
        {
            currentBar = (int)Mathf.Repeat(currentBar, triggerChances.Count);
            return triggerChances[currentBar] > Random.Range(0, 100);
        }

        public void Scrub(int direction)
        {
            var stepsArray = new AnysongPatternStep[16];
            for (int i = 0; i < 16; i++)
            {
                var index = (int)Mathf.Repeat(i + direction, 16);
                stepsArray[i] = steps[index];
            }

            steps.Clear();
            steps.AddRange(stepsArray);
        }

        public void SetPatternLength(int newLength)
        {
            patternLength = newLength;
        }


        public void Reset()
        {
            _internalIndex = 0;
        }

        public void Advance()
        {
            _internalIndex++;
            _internalIndex = (int)Mathf.Repeat(_internalIndex, patternLength);
        }

        public AnysongPatternStep GetStep(int stepIndex)
        {
            return steps[stepIndex];
        }


        public void SetStepIndex(int stepIndex)
        {
            _internalIndex = stepIndex;
        }

        public AnysongPatternStep GetCurrentStep()
        {
            if (steps.Count == 0)
            {
                Init();
            }

            return steps[_internalIndex];
        }

        public void RandomizeMelody()
        {
            List<int> notes = new List<int>();
            foreach (var patternStep in steps)
            {
                if (patternStep.NoteOn)
                {
                    notes.Add(patternStep.rootNote);
                }
            }

            foreach (var patternStep in steps)
            {
                if (patternStep.NoteOn)
                {
                    int thisIndex = Random.Range(0, notes.Count);
                    patternStep.rootNote = notes[thisIndex];
                    notes.RemoveAt(thisIndex);
                }
            }
        }

        public void RandomizeRhythm()
        {
            List<int> notes = new List<int>();
            foreach (var patternStep in steps)
            {
                if (patternStep.NoteOn)
                {
                    notes.Add(patternStep.rootNote);
                    //patternStep.noteOn = false;
                }
            }

            while (notes.Count > 0)
            {
                var thisStep = steps[Random.Range(0, 16)];
                if (!thisStep.NoteOn)
                {
                    //thisStep.noteOn = true;
                    thisStep.rootNote = notes[0];
                    notes.RemoveAt(0);
                }
            }
        }

        public void Clear()
        {
            foreach (var step in steps)
            {
                step.Init();
            }
        }

        public void SyncToClock()
        {
            _internalIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.Sub16, patternLength);
        }
    }
}