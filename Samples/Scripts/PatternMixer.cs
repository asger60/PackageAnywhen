using System;
using Anywhen;
using PackageAnywhen.Runtime.Anywhen;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class PatternMixer : MonoBehaviour
    {
        public PatternCollection savePatternCollection;
        [Range(0, 3f)] public float currentPatternMix;

        public AnimationCurve mixCurve;

        [Serializable]
        public struct Pattern
        {
            [Range(0, 1f)] public float currentWeight;

            public StepPattern[] patternTracks;
            [Range(0, 1f)] public float swing;
            [Range(0, 1f)] public float humanize;

            public void OnTick(AnywhenMetronome.TickRate tickRate)
            {
                int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);

                foreach (var patternTrack in patternTracks)
                {
                    if (patternTrack.steps[stepIndex].noteOn)
                    {
                        if (patternTrack.steps[stepIndex].stepWeight > currentWeight) return;

                        NoteEvent note = new NoteEvent(0, NoteEvent.EventTypes.NoteOn,
                            patternTrack.steps[stepIndex].accent ? 1 : 0.5f,
                            AnywhenMetronome.GetTiming(tickRate, swing, humanize));
                        EventFunnel.HandleNoteEvent(note, patternTrack.instrument, tickRate);
                    }
                }
            }

            public bool ShouldTrigger(int trackIndex, int stepIndex)
            {
                if (currentWeight <= 0) return false;
                //if (!patternTrack.steps[stepIndex].noteOn) continue;
                return patternTracks[trackIndex].steps[stepIndex].noteOn && (currentWeight > patternTracks[trackIndex].steps[stepIndex].stepWeight) ;
            }
        }

        public Pattern[] patterns;
        public AnywhenMetronome.TickRate tickRate;

        private void Start()
        {
            AnywhenMetronome.Instance.OnTick16 += OnTick32;
        }

        private void OnTick32()
        {
            foreach (var pattern in patterns)
            {
                pattern.OnTick(tickRate);
            }
        }

        private void Update()
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                patterns[i].currentWeight = Mathf.Lerp(1, 0, mixCurve.Evaluate(Mathf.Abs(i - currentPatternMix)));
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
#endif
    }
}