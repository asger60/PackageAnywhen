using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnyPattern
    {
        
        
        
        public List<float> triggerChances = new List<float>();
        
        
        
        
        
        public List<AnyPatternStep> steps;
        public int rootNote = 0;
        [Range(0, 16)] public int patternLength = 16;
        private int _internalIndex;
        public int InternalIndex => _internalIndex;

        public void Init()
        {
            triggerChances.AddRange(new[] { 0f, 0f, 0f, 0f });

            steps = new List<AnyPatternStep>();
            for (int i = 0; i < 16; i++)
            {
                var newStep = new AnyPatternStep();
                newStep.Init();
                steps.Add(newStep);
            }
        }

        public AnyPattern Clone()
        {
            var clone = new AnyPattern
            {
                steps = new List<AnyPatternStep>()
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
            return triggerChances[currentBar] > Random.Range(0,100);
        }

        public void Scrub(int direction)
        {
            Debug.Log("Scrub " + direction);
            var stepsArray = new AnyPatternStep[16];
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

        public AnyPatternStep GetCurrentStep()
        {
            return steps[_internalIndex];
        }
    }
}