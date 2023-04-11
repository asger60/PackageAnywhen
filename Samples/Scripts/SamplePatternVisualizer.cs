using System;
using System.Collections.Generic;
using Anywhen;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PackageAnywhen.Samples.Scripts
{
    public class SamplePatternVisualizer : MonoBehaviour
    {
        public Vector3[] circlePositions = new Vector3[16];


        [Serializable]
        public class Steps
        {
            private GameObject _gameObject;
            private int _connectedStepIndex;
            private int _currentStepIndex;
            private SamplePatternVisualizer _samplePatternVisualizer;

            public void Init(SamplePatternVisualizer samplePatternVisualizer, int connectedStep, GameObject stepObject)
            {
                _connectedStepIndex = connectedStep;
                _gameObject = stepObject;
                _currentStepIndex = _connectedStepIndex;
                _samplePatternVisualizer = samplePatternVisualizer;
            }

            public void Tick()
            {
                _currentStepIndex = (int)Mathf.Repeat(_currentStepIndex, 16);
                _gameObject.transform.position = _samplePatternVisualizer.circlePositions[_currentStepIndex];
                _currentStepIndex++;
            }
        }

        public GameObject stepPrefab;
        public List<Steps> steps;

        private void Start()
        {
            for (var i = 0; i < 16; i++)
            {
                var step = new Steps();
                var stepObject = Instantiate(stepPrefab, transform);
                stepObject.transform.localScale = Vector3.one * Random.Range(0, 1f);
                step.Init(this, i, stepObject);
                AnywhenMetronome.Instance.OnTick16 += step.Tick;
                steps.Add(step);
            }
        }


        [ContextMenu("generate positions")]
        void GeneratePositions()
        {
            for (int i = 0; i < circlePositions.Length; i++)
            {
                var x = (5 * Mathf.Cos((i / 16f * 360) / (180f / Mathf.PI)));
                var z = (5 * Mathf.Sin((i / 16f * 360) / (180f / Mathf.PI)));

                circlePositions[i] = new Vector3(-x, 0, z);
            }
        }
    }
}