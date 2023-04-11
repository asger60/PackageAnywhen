using System;
using System.Collections.Generic;
using Anywhen;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class SamplePatternVisualizer : MonoBehaviour
    {
        public Vector3[] circlePositions = new Vector3[16];


        [Serializable]
        public class Steps
        {
            private GameObject _gameObject;
            public int _connectedStepIndex;
            public int _currentStepIndex;
            private SamplePatternVisualizer _samplePatternVisualizer;
            public bool noteOn;

            public void Init(SamplePatternVisualizer samplePatternVisualizer, int connectedStep, GameObject stepObject)
            {
                _samplePatternVisualizer = samplePatternVisualizer;
                _connectedStepIndex = (int)Mathf.Repeat(connectedStep, 16);
                _gameObject = stepObject;
                _currentStepIndex = _samplePatternVisualizer.circleStepLength - connectedStep;
            }

            public void Tick()
            {
                _currentStepIndex = (int)Mathf.Repeat(_currentStepIndex, _samplePatternVisualizer.circleStepLength);
                _gameObject.transform.position = _samplePatternVisualizer.circlePositions[_currentStepIndex];

                _gameObject.transform.localScale = noteOn ? Vector3.one * 0.4f : Vector3.one * 0.05f;

                if (noteOn && (int)Mathf.Repeat(_currentStepIndex, 16) == 0)
                {
                    _gameObject.transform.position += Vector3.up;
                    _gameObject.transform.localScale = Vector3.one * 0.7f;

                }   


                _currentStepIndex++;
            }

            public void SetNoteOn(bool stepTrigger)
            {
                noteOn = stepTrigger;
            }
        }

        public int circleStepLength = 16;
        public float circleDistance = 5;
        public GameObject stepPrefab;
        public List<Steps> steps;
        public PatternMixer patternMixer;
        public int trackIndex;

        private void Start()
        {
            circlePositions = new Vector3[circleStepLength];
            GeneratePositions();
            for (var i = 0; i < circleStepLength; i++)
            {
                var step = new Steps();
                var stepObject = Instantiate(stepPrefab, transform);
                stepObject.transform.localScale = Vector3.one * Random.Range(0, 1f);
                step.Init(this, i, stepObject);
                AnywhenMetronome.Instance.OnTick16 += step.Tick;
                steps.Add(step);
            }
        }

        private void Update()
        {
            var stepTriggers = patternMixer.GetCurrentPattern(trackIndex);
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                step.SetNoteOn(stepTriggers[(int)Mathf.Repeat(i, 16)]);
            }
        }

        [ContextMenu("generate positions")]
        void GeneratePositions()
        {
            circlePositions = new Vector3[circleStepLength];
            for (int i = 0; i < circlePositions.Length; i++)
            {
                var x = (circleDistance * Mathf.Cos((i / (float)circleStepLength * 360) / (180f / Mathf.PI)));
                var z = (circleDistance * Mathf.Sin((i / (float)circleStepLength * 360) / (180f / Mathf.PI)));

                circlePositions[i] = new Vector3(-x, 0, z);
            }
        }
    }
}