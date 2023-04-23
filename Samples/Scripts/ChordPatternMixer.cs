using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Samples.Scripts;
using UnityEditor;
using UnityEngine;
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
        private ChordPatternVisualizer _chordPatternVisualizer;
        [SerializeField] private MixView mixView;
        public int octave = 0;
        private readonly int[] _goodNotes = new[] { 2, 4, 5, 6, -2, -1, 8 };

        private readonly int[] _rootNotes = new[] { 0,  2,  4 };

        private readonly int[][] _goodBasicChords =
        {
            new[]
            {
                0, 2
            },
            new[]
            {
                0, 4,
            },
            new[]
            {
                0, 5,
            },
            new[]
            {
                0, 3,
            }
        };

        private readonly int[][] _goodSimpleChords =
        {
            new[]
            {
                0, 2, 4,
            },
            new[]
            {
                0, 2, 5,
            },
            new[]
            {
                0, 1, 4,
            },
            new[]
            {
                0, 3, 4,
            }
        };

        private readonly int[][] _goodComplexChords =
        {
            new[]
            {
                0, 2, 4, -1,
            },
            new[]
            {
                0, 2, 4, 6,
            },
            new[]
            {
                0, 1, 4, 5,
            },
        };


        public bool[] CurrentTriggerPattern => _currentTriggerPattern;
        public int[][] CurrentChordPattern => _currentChordPattern;
        public int[] CurrentNotePattern => _currentNotePattern;
        private bool[] _currentTriggerPattern = new bool[32];
        private int[][] _currentChordPattern = new int[32][];
        private int[] _currentNotePattern = new int[32];

        private int _barsCounter;
        public AnywhenInstrument instrument;

        float[] _currentTriggerMixValue = new float[32];
        float[] _currentNoteMixValue = new float[32];


        private void Start()
        {
            _currentTriggerMixValue = new float[32];
            _currentNoteMixValue = new float[32];
            TrackHandler.Instance.AttachMixInterface(this, 2);
            AnywhenMetronome.Instance.OnTick16 += OnTick;
            _chordPatternVisualizer = GetComponent<ChordPatternVisualizer>();
        }


        private void OnTick()
        {
            int stepIndex = (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(tickRate), 16);
            if (stepIndex == 0)
                _barsCounter++;

            _barsCounter = (int)Mathf.Repeat(_barsCounter, 2);
            int currentStep = stepIndex + (_barsCounter * 16);


            if (_currentTriggerPattern[currentStep])
            {
                var n = _currentChordPattern[currentStep];
                if (n.Length > 0)
                {
                    var root = _currentNotePattern[currentStep];
                    for (var i = 0; i < n.Length; i++)
                    {
                        n[i] += root + (octave * 7);
                    }


                    NoteEvent note = new NoteEvent(NoteEvent.EventTypes.NoteOn,
                        AnywhenMetronome.GetTiming(tickRate, 0, 0),
                        n, new double[] { 0, 0, 0, 0, 0 }, 0, 0, 1);

                    EventFunnel.HandleNoteEvent(note, instrument, tickRate);
                }
            }
        }

        int[] GetChordFromString(string s)
        {
            return Array.ConvertAll(s.Split(','), int.Parse);
        }

        private void Update()
        {
            GetCurrentTriggerPattern();
            GetCurrentChordPattern();
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


        private void GetCurrentChordPattern()
        {
            for (int i = 0; i < 32; i++)
            {
                float weightDistance = 100;
                for (var index = 0; index < chordPatterns.Length; index++)
                {
                    float thisWeightDistance =
                        (Mathf.Abs(_currentNoteMixValue[i] - index) -
                         (chordPatterns[index].pattern.steps[i].stepWeight));

                    if (thisWeightDistance < weightDistance)
                    {
                        weightDistance = thisWeightDistance;
                        _currentChordPattern[i] = GetChordFromString(chordPatterns[index].pattern.steps[i].chord);
                        _currentNotePattern[i] = chordPatterns[index].pattern.steps[i].note;
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
                int width = i;
                for (var index1 = 0; index1 < chordPatterns[i].pattern.steps.Length; index1++)
                {
                    chordPatterns[i].pattern.steps[index1].note = GetRootNote(width);

                    int[][] chordArray = null;
                    if (i == 0)
                        chordArray = _goodBasicChords;
                    if (i == 1)
                        chordArray = _goodSimpleChords;
                    if (i == 2)
                        chordArray = _goodComplexChords;

                    chordPatterns[i].pattern.steps[index1].chord = CreateChord(chordArray, width);
                    chordPatterns[i].pattern.steps[index1].stepWeight = Random.Range(0, 1f);
                }
            }
        }

        int GetRootNote(int width)
        {
            //return 0;
            int i = (int)Mathf.Repeat(Random.Range(0, width * 2), _rootNotes.Length);
            return _rootNotes[i];
        }

        string CreateChord(int[][] chordArray, int width)
        {
            int[] chord = chordArray[Random.Range(0, chordArray.Length)];

            //chord[0] = 0;
            //for (var index = 1; index < chord.Length; index++)
            //{
            //    chord[index] = GetRandomNote(index * 2, new List<int>(chord));
            //}

            string s = "";
            foreach (var c in chord)
            {
                print(c);
                s += c + ",";
            }

            print("break");
            s = s.Remove(s.Length - 1, 1);
            return s;
        }


        int GetRandomNote(int width, List<int> ignoreNotes)
        {
            //width = Mathf.Min(width, _goodNotes.Length);
            int n = Random.Range(-width, width);
            int returnNote = _goodNotes[(int)Mathf.Repeat(n, _goodNotes.Length)];
            int i = 0;
            while (ignoreNotes.Contains(returnNote) && i < 10)
            {
                returnNote = _goodNotes[Random.Range(0, width)];
                i++;
            }

            return returnNote;
        }

#endif

        public float mixPower = 0.5f;

        void MixTriggers(int mixIndex, int stepIndex)
        {
            float add = mixIndex == 1 ? -mixPower : mixPower;
            int range = 3;
            for (int i = stepIndex - range; i < stepIndex + range; i++)
            {
                int paintIndex = (int)Mathf.Repeat(i, 32);
                float effect = Mathf.InverseLerp(range, 0f, Mathf.Abs(stepIndex - paintIndex));
                _currentTriggerMixValue[paintIndex] += add * effect;
                _currentTriggerMixValue[paintIndex] =
                    Mathf.Clamp(_currentTriggerMixValue[paintIndex], 0, triggerPatterns.Length);
            }
        }

        void MixNotes(int mixIndex, int stepIndex)
        {
            mixIndex -= 2;
            float add = mixIndex == 0 ? -mixPower : mixPower;
            int range = 3;
            for (int i = stepIndex - range; i < stepIndex + range; i++)
            {
                int paintIndex = (int)Mathf.Repeat(i, 32);
                float effect = Mathf.InverseLerp(range, 0f, Mathf.Abs(stepIndex - paintIndex));
                _currentNoteMixValue[paintIndex] += add * effect;
                _currentNoteMixValue[paintIndex] =
                    Mathf.Clamp(_currentNoteMixValue[paintIndex], 0, chordPatterns.Length);
            }
        }

        public void Mix(int patternIndex, int stepIndex)
        {
            if (patternIndex < 2)
                MixTriggers(patternIndex, stepIndex);
            else
            {
                MixNotes(patternIndex, stepIndex);
            }

            float[] values = new float[4];

            int currentTriggerCount = 0;
            foreach (var b in _currentTriggerPattern)
            {
                if (b) currentTriggerCount++;
            }

            float totalNoteMix = 0;
            foreach (var f in _currentNoteMixValue)
            {
                totalNoteMix += f;
            }

            float noteMixPercent = (totalNoteMix / chordPatterns.Length) / 32f;

            values[0] = (currentTriggerCount / 32f) / 2f;
            values[1] = (1 - (currentTriggerCount / 32f)) / 2f;


            values[2] = (1 - noteMixPercent) / 2f;
            values[3] = noteMixPercent / 2f;

            mixView.UpdateValues(values);
        }

        public void SetIsActive(bool state)
        {
            _chordPatternVisualizer.SetIsTrackActive(state);
        }
    }
}