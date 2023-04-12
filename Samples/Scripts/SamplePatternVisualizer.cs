using System;
using System.Collections.Generic;
using Anywhen;
using Unity.VisualScripting;
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
            private Vector3 _currentPosition, _nextPosition;
            private float _stepTimer;
            private float _stepDuration;
            private MaterialPropertyBlock _materialPropertyBlock;
            private Renderer _objectRenderer;
            private Vector3 _idleScale;
            private Vector3 _idlePosition;
            private float _wobbleSpeed;
            private float _wobbleAmount;

            public void Init(SamplePatternVisualizer samplePatternVisualizer, int connectedStep, GameObject stepObject,
                Color color)
            {
                _gameObject = stepObject;
                _objectRenderer = _gameObject.GetComponent<Renderer>();
                _materialPropertyBlock = new MaterialPropertyBlock();
                SetColor(color);
                _samplePatternVisualizer = samplePatternVisualizer;
                _connectedStepIndex = (int)Mathf.Repeat(connectedStep, 16);

                _currentStepIndex = _samplePatternVisualizer.circleStepLength - connectedStep;
                _stepDuration = (float)AnywhenMetronome.Instance.GetLength(_samplePatternVisualizer.tickRate);
                _gameObject.transform.position =
                    _samplePatternVisualizer.circlePositions[
                        (int)Mathf.Repeat(_currentStepIndex, (int)_samplePatternVisualizer.tickRate)];
                _idleScale = noteOn ? Vector3.one * 0.4f : Vector3.one * 0.05f;
                _idlePosition =
                    _samplePatternVisualizer.circlePositions[
                        (int)Mathf.Repeat(_currentStepIndex, (int)_samplePatternVisualizer.tickRate)];
                _wobbleSpeed = Random.Range(1, 3f);
                _wobbleAmount = Random.Range(0.05f, 0.2f);
            }

            public void Tick()
            {
                _currentStepIndex = (int)Mathf.Repeat(_currentStepIndex, (int)_samplePatternVisualizer.tickRate);
                
                _currentPosition = _samplePatternVisualizer.circlePositions[_currentStepIndex];
                _nextPosition =
                    _samplePatternVisualizer.circlePositions[
                        (int)Mathf.Repeat(_currentStepIndex + 1, (int)_samplePatternVisualizer.tickRate)];
                _stepTimer = 0;

                if (noteOn && (int)Mathf.Repeat(_currentStepIndex, (int)_samplePatternVisualizer.tickRate) == 0)
                {
                    _gameObject.transform.position += Vector3.up;
                    _gameObject.transform.localScale = Vector3.one * 0.7f;
                }


                _currentStepIndex++;
            }

            public void SetNoteOn(bool stepTrigger)
            {
                noteOn = stepTrigger;
                _idleScale = noteOn ? Vector3.one * 0.4f : Vector3.one * 0.3f;
            }

            public void SetColor(Color color)
            {
                _materialPropertyBlock.SetColor("_Color", color);
                _objectRenderer.SetPropertyBlock(_materialPropertyBlock);
            }

            public void Update()
            {
                _stepTimer += Time.deltaTime;
                _gameObject.transform.localScale =
                    Vector3.Lerp(_gameObject.transform.localScale, _idleScale, Time.deltaTime * 5);
                Vector3 wobbleAdd = Vector3.up * (Mathf.Sin(Time.time * _wobbleSpeed) * _wobbleAmount);
                if (!noteOn)
                    wobbleAdd = Vector3.zero;
                //_gameObject.transform.position =
                //    Vector3.Lerp(_gameObject.transform.position,
                //        _idlePosition + wobbleAdd,
                //        Time.deltaTime * 5);

                
            }
        }

        public Color color = Color.white;
        public int circleStepLength = 16;
        public float circleDistance = 5;
        public PartyType stepPrefab;
        public List<Steps> steps;
        public PatternMixer patternMixer;
        public int trackIndex;
        public AnywhenMetronome.TickRate tickRate;
        private List<PartyType> _partyTypes = new();
        public GameObject groundTilePrefab;
        private void Start()
        {
            GeneratePositions();
            for (var i = 0; i < 16; i++)
            {
                var step = new Steps();
                var stepObject = Instantiate(stepPrefab, transform);
                var groundObject = Instantiate(groundTilePrefab, transform);
                groundObject.transform.position = circlePositions[i];
                _partyTypes.Add(stepObject);
                stepObject.Init(i, this);
                stepObject.transform.localScale = Vector3.one * Random.Range(0, 1f);
                step.Init(this, i, stepObject.gameObject, color);
                switch (tickRate)
                {
                    case AnywhenMetronome.TickRate.None:
                        break;
                    case AnywhenMetronome.TickRate.Sub2:
                        AnywhenMetronome.Instance.OnTick2 += step.Tick;
                        break;
                    case AnywhenMetronome.TickRate.Sub4:
                        AnywhenMetronome.Instance.OnTick4 += step.Tick;
                        break;
                    case AnywhenMetronome.TickRate.Sub8:
                        AnywhenMetronome.Instance.OnTick8 += step.Tick;
                        break;
                    case AnywhenMetronome.TickRate.Sub16:
                        AnywhenMetronome.Instance.OnTick16 += step.Tick;
                        break;
                    case AnywhenMetronome.TickRate.Sub32:
                        AnywhenMetronome.Instance.OnTick32 += step.Tick;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                steps.Add(step);
            }
        }

        private void Update()
        {
            var stepTriggers = patternMixer.GetCurrentPattern(trackIndex);
            var instruments = patternMixer.GetInstruments(trackIndex);
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                step.SetNoteOn(stepTriggers[(int)Mathf.Repeat(i, 16)]);
                step.SetColor(instruments[(int)Mathf.Repeat(i, 16)].color);
                step.Update();
                _partyTypes[i].SetNoteOn(stepTriggers[(int)Mathf.Repeat(i, 16)]);
            }
        }

        [ContextMenu("generate positions")]
        void GeneratePositions()
        {
            circlePositions = new Vector3[(int)tickRate];
            for (int i = 0; i < circlePositions.Length; i++)
            {
                var x = (circleDistance * Mathf.Cos((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));
                var z = (circleDistance * Mathf.Sin((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));

                circlePositions[i] = new Vector3(x, 0, z);
            }
        }
    }
}