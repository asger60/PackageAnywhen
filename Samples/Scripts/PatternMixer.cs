using System;
using Anywhen;
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
                        if (Random.Range(0, 1f) > currentWeight) return;

                        NoteEvent note = new NoteEvent(0, NoteEvent.EventTypes.NoteOn,
                            patternTrack.steps[stepIndex].accent ? 1 : 0.5f,
                            AnywhenMetronome.GetTiming(tickRate, swing, humanize));
                        EventFunnel.HandleNoteEvent(note, patternTrack.instrument, tickRate);
                    }
                }
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
                patterns[i].currentWeight = Mathf.Lerp(1, 0,   mixCurve.Evaluate(Mathf.Abs(i-currentPatternMix)));
            }
        }

        [ContextMenu("SavePattern")]
        void SavePattern()
        {
            savePatternCollection.patterns = patterns;
        }
        [ContextMenu("LoadPattern")]
        void LoadPattern()
        {
            patterns = savePatternCollection.patterns;
        }
    }
}