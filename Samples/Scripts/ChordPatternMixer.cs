using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class ChordPatternMixer : MonoBehaviour, IMixableObject
    {
        public PatternCollection savePatternCollection;


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


        public DrumPatternMixer.PatternInstrument[] patternInstruments;

        public BassPattern[] triggerPatterns;
        public BassPattern[] chordPatterns;
        public AnywhenMetronome.TickRate tickRate;
        private BassPatternVisualizer _bassPatternVisualizer;
        [SerializeField] private MixView mixView;
        private readonly int[] _goodNotes = new[] { 2, 4, 5, 6, -2 };

        private readonly bool[] _currentPattern = new bool[32];
        private readonly int[][] _currentNotePattern = new int[32][];
        private int _barsCounter;
        public AnywhenInstrument instrument;


        private void Start()
        {
            TrackHandler.Instance.AttachMixInterface(this, 2);
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
                var n = GetChordForStep(currentStep);

                if (n.Length > 0)
                {
                    NoteEvent note = new NoteEvent(NoteEvent.EventTypes.NoteOn,
                        AnywhenMetronome.GetTiming(tickRate, 0, 0),
                        n, new double[] { 0, 0, 0, 0,0 }, 0, 0, 1);

                    EventFunnel.HandleNoteEvent(note, instrument, tickRate);
                }
            }
        }

        int[] GetChordForStep(int stepIndex)
        {
            int[] note = new[] { 0 };
            float bestWeight = 0;
            foreach (var melodyPattern in chordPatterns)
            {
                float thisWeight = melodyPattern.currentWeight * melodyPattern.pattern.steps[stepIndex].stepWeight;
                if (thisWeight > bestWeight)
                {
                    note = GetChordFromString(melodyPattern.pattern.steps[stepIndex].chord);
                    bestWeight = thisWeight;
                }
            }

            return note;
        }

        int[] GetChordFromString(string s)
        {
            return Array.ConvertAll(s.Split(','), int.Parse);
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


        public int[][] GetCurrentNotePattern(int trackIndex)
        {
            //foreach (var pattern in melodyPatterns)
            {
                for (int i = 0; i < 32; i++)
                {
                    _currentNotePattern[i] = GetChordForStep(i);
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

        [ContextMenu("create chords")]
        void CreateChords()
        {
            for (int i = 0; i < chordPatterns.Length; i++)
            {
                for (var index1 = 0; index1 < chordPatterns[i].pattern.steps.Length; index1++)
                {
                    int rnd = Random.Range(0, i + 1);
                    switch (rnd)
                    {
                        case 0:
                            chordPatterns[i].pattern.steps[index1].chord = CreateChord();
                            break;
                        case 1:
                            chordPatterns[i].pattern.steps[index1].chord = CreateChord();
                            break;
                        case 2:
                            chordPatterns[i].pattern.steps[index1].chord = CreateChord();
                            break;
                        case 3:
                            chordPatterns[i].pattern.steps[index1].chord = CreateChord();
                            break;
                        case 4:


                            break;
                    }


                    chordPatterns[i].pattern.steps[index1].stepWeight = Random.Range(0, 1f);
                }
            }
        }

        string CreateChord()
        {
            int[] chord = new int[Random.Range(2, 6)];
            chord[0] = 0;
            for (var index = 1; index < chord.Length; index++)
            {
                chord[index] = GetRandomNote(index * 2);
            }

            string s = "";
            foreach (var c in chord)
            {
                s += c + ",";
            }

            s = s.Remove(s.Length - 1, 1);
            return s;
        }


        int GetRandomNote(int width)
        {
            //int rnd = Random.Range(0, 5);
            //if (rnd == 0) return 0;

            width = Mathf.Min(width, _goodNotes.Length);
            return _goodNotes[Random.Range(0, width)];
        }

#endif

        void MixTriggers(int mixIndex)
        {
            float combinedWeight = 0;

            triggerPatterns[mixIndex].currentWeight += 0.05f;
            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                combinedWeight += triggerPatterns[i].currentWeight;
            }

            if (combinedWeight > 1)
            {
                float subtract = (combinedWeight - 1);
                for (int i = 0; i < triggerPatterns.Length; i++)
                {
                    if (i != mixIndex)
                        triggerPatterns[i].currentWeight -= subtract;
                }
            }

            for (int i = 0; i < triggerPatterns.Length; i++)
            {
                triggerPatterns[i].currentWeight = Mathf.Clamp01(triggerPatterns[i].currentWeight);
            }
        }

        void MixRange(int mixIndex)
        {
            mixIndex -= 2;
            float combinedWeight = 0;

            chordPatterns[mixIndex].currentWeight += 0.05f;
            for (int i = 0; i < chordPatterns.Length; i++)
            {
                combinedWeight += chordPatterns[i].currentWeight;
            }

            if (combinedWeight > 1)
            {
                float subtract = (combinedWeight - 1);
                for (int i = 0; i < chordPatterns.Length; i++)
                {
                    if (i != mixIndex)
                        chordPatterns[i].currentWeight -= subtract;
                }
            }

            for (int i = 0; i < chordPatterns.Length; i++)
            {
                chordPatterns[i].currentWeight = Mathf.Clamp01(chordPatterns[i].currentWeight);
            }
        }

        public void Mix(int patternIndex, int stepIndex)
        {
            print("mixing chords");
            if (patternIndex < 2)
                MixTriggers(patternIndex);
            else
            {
                MixRange(patternIndex);
            }

            float[] values = new float[4];

            values[0] = triggerPatterns[0].currentWeight / 2f;
            values[1] = triggerPatterns[1].currentWeight / 2f;
            values[2] = chordPatterns[0].currentWeight / 2f;
            values[3] = chordPatterns[1].currentWeight / 2f;

            mixView.UpdateValues(values);
        }

        public void SetIsActive(bool state)
        {
            if (_bassPatternVisualizer != null)
                _bassPatternVisualizer.SetIsTrackActive(state);
        }
    }
}