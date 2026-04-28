using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [Serializable]
    public struct AnysongSectionTrack
    {
        public enum PatternProgressionType
        {
            Sequence,
            WeightedRandom,
            Random,
        }

        public PatternProgressionType patternProgressionType;
        //public AnysongTrackSettings anysongTrackSettings;


        public List<AnysongPattern> patterns;

        private AnysongPattern _currentPattern;
        private int _currentPatternBar;
        int _currentPatternIndex;

        public struct Unmanaged
        {
            public PatternProgressionType patternProgressionType;
            public AnysongTrackSettings.Unmanaged anysongTrackSettings;
            public NativeArray<AnysongPattern.Unmanaged> patterns;
            public AnysongPattern.Unmanaged currentPattern;
            public int currentPatternBar;
            public int currentPatternIndex;
        }

        public Unmanaged ToUnmanaged()
        {
            var unmanagedPatterns = new AnysongPattern.Unmanaged[patterns.Count];
            for (int i = 0; i < patterns.Count; i++)
                unmanagedPatterns[i] = patterns[i].ToUnmanaged();

            return new Unmanaged
            {
                patternProgressionType = patternProgressionType,
                //anysongTrackSettings = anysongTrackSettings.ToUnmanaged(),
                patterns = new NativeArray<AnysongPattern.Unmanaged>(unmanagedPatterns, Allocator.Persistent),
                currentPattern = _currentPattern.ToUnmanaged(),
                currentPatternBar = _currentPatternBar,
                currentPatternIndex = _currentPatternIndex,
            };
        }

        public void Init(AnysongTrackSettings songSongTrackSettings)
        {
            //anysongTrackSettings = songSongTrackSettings;
            patterns = new List<AnysongPattern>(1);
            for (var i = 0; i < patterns.Count; i++)
            {
                patterns[i] = new AnysongPattern();
                patterns[i].Init();
            }
        }


        public AnysongSectionTrack Clone()
        {
            var clone = new AnysongSectionTrack
            {
                patterns = new List<AnysongPattern>(patterns.Count),
            };

            for (var i = 0; i < patterns.Count; i++)
            {
                clone.patterns[i] = patterns[i].Clone();
            }

            //clone.anysongTrackSettings = anysongTrackSettings;
            clone.patternProgressionType = patternProgressionType;


            return clone;
        }

        public AnysongPattern GetPattern(int index)
        {
            if (index >= patterns.Count)
                return patterns[0];

            return patterns[index];
        }

        public AnysongPattern GetPlayingPattern()
        {
            if (patterns.Count == 0)
            {
                patterns = new List<AnysongPattern>(1);
                patterns[0] = new AnysongPattern();
                patterns[0].Init();
            }

            return _currentPattern;
        }


        public void AdvancePlayingPattern()
        {
            _currentPatternBar++;
            _currentPatternIndex = GetProgressionPatternIndex(_currentPatternBar);
            _currentPattern = GetPattern(_currentPatternIndex);
            _currentPattern.SetStepIndex(0);
        }

        int GetProgressionPatternIndex(int patternBar)
        {
            int patternIndex = (int)Mathf.Repeat(patternBar, patterns.Count);
            switch (patternProgressionType)
            {
                case PatternProgressionType.Sequence:

                    break;
                case PatternProgressionType.WeightedRandom:
                    bool didFindPattern = false;
                    float totalWeight = 0;

                    // First pass: calculate total weight
                    for (var i = 0; i < patterns.Count; i++)
                    {
                        var anyPattern = patterns[i];
                        float thisTriggerChance = anyPattern.triggerChances[(int)Mathf.Repeat(patternBar, 4)];
                        totalWeight += thisTriggerChance;
                    }

                    // If total weight is 0, fallback to first pattern
                    if (totalWeight <= 0)
                    {
                        patternIndex = 0;
                        didFindPattern = true;
                    }
                    else
                    {
                        // Generate random number within total weight range
                        float randomValue = Random.Range(0f, totalWeight);
                        float currentWeight = 0;

                        // Second pass: find the selected pattern
                        for (var i = 0; i < patterns.Count; i++)
                        {
                            var anyPattern = patterns[i];
                            float thisTriggerChance = anyPattern.triggerChances[(int)Mathf.Repeat(patternBar, 4)];
                            currentWeight += thisTriggerChance;

                            if (randomValue <= currentWeight)
                            {
                                patternIndex = i;
                                didFindPattern = true;
                                break;
                            }
                        }
                    }

                    if (!didFindPattern)
                    {
                        patternIndex = 0;
                    }

                    break;
                case PatternProgressionType.Random:
                    patternIndex = Random.Range(0, patterns.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return patternIndex;
        }


        public void Reset()
        {
            _currentPatternBar = 0;
            if (patterns.Count > 0)
            {
                _currentPatternIndex = GetProgressionPatternIndex(_currentPatternBar);
                _currentPattern = GetPattern(_currentPatternIndex);
            }

            foreach (var pattern in patterns)
            {
                pattern.Reset();
            }
        }

        public void SyncToClock()
        {
            _currentPatternIndex = GetProgressionPatternIndex(AnywhenMetronome.Instance.CurrentBar);
            _currentPattern = GetPattern(_currentPatternIndex);
            _currentPattern.SyncToClock();
        }

        public void SetTrack(AnysongTrackSettings songTrack)
        {
            //anysongTrackSettings = songTrack;
        }

        public int GetPlayingPatternIndex()
        {
            return _currentPatternIndex;
        }

        public bool IsNull()
        {
            return (patterns.Count == 0);
        }
    }
}