using System;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class DrumPatternMixer : MonoBehaviour, IMixableObject
    {
        public PatternCollection savePatternCollection;
        [Range(0, 3f)] public float currentPatternMix;
        [Range(0, 3f)] public float currentInstrumentMix;


        public AnimationCurve mixCurve;
        public MixView mixView;

        [Serializable]
        public struct Pattern
        {
            [Range(0, 1f)] public float currentWeight;

            public StepPattern[] patternTracks;

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


        private void Start()
        {
            TrackHandler.Instance.AttachMixInterface(this, 0);
            AnywhenMetronome.Instance.OnTick16 += OnTick;
            _patternVisualizers = GetComponentsInChildren<SamplePatternVisualizer>();
        }

        public void Mix(int patternIndex, int stepIndex)
        {
            float combinedWeight = 0;
            patterns[patternIndex].currentWeight += 0.05f;
            float[] values = new float[4];
            for (int i = 0; i < patterns.Length; i++)
            {
                combinedWeight += patterns[i].currentWeight;
            }

            if (combinedWeight > 1)
            {
                float subtract = (combinedWeight - 1) / 3f;
                for (int i = 0; i < patterns.Length; i++)
                {
                    if (i != patternIndex)
                        patterns[i].currentWeight -= subtract;
                }
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                patterns[i].currentWeight = Mathf.Clamp01(patterns[i].currentWeight);
                patternInstruments[i].currentWeight = patterns[i].currentWeight;
                values[i] = patterns[i].currentWeight;
            }

            mixView.UpdateValues(values);
        }

        public void SetIsActive(bool state)
        {
            foreach (var samplePatternVisualizer in _patternVisualizers)
            {
                samplePatternVisualizer.SetIsTrackActive(state);
            }
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

        private SamplePatternVisualizer[] _patternVisualizers;

        public void SetPartyDudesActive(bool state)
        {
            foreach (var samplePatternVisualizer in _patternVisualizers)
            {
                samplePatternVisualizer.SetIsTrackActive(state);
            }
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

    public interface IMixableObject
    {
        public void Mix(int currentPattern, int stepIndex);
        public void SetIsActive(bool state);
    }
}