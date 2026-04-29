using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongSectionTrack
    {
        public enum PatternProgressionType
        {
            Sequence,
            WeightedRandom,
            Random,
        }

        public PatternProgressionType patternProgressionType;
        public List<AnysongPattern> patterns;
        private AnysongPattern _currentPattern;
        private int _currentPatternBar;
        int _currentPatternIndex;


        public struct Unmanaged
        {
            public PatternProgressionType PatternProgressionType;
            public AnysongTrackSettings.Unmanaged AnysongTrackSettings;
            public NativeArray<AnysongPattern.Unmanaged> Patterns;
            public AnysongPattern.Unmanaged CurrentPattern;
            public int CurrentPatternBar;
            public int CurrentPatternIndex;
            public Unity.Mathematics.Random Random;


            public void AdvancePlayingPattern()
            {
                CurrentPatternBar++;
                CurrentPatternIndex = GetProgressionPatternIndex(CurrentPatternBar, Patterns, PatternProgressionType, ref Random);
                CurrentPattern = Patterns[CurrentPatternIndex];
                CurrentPattern.SetStepIndex(0);
            }

            public AnysongPattern.Unmanaged GetCurrentPattern()
            {
                return CurrentPattern;
            }

            public void Sync(int index)
            {
                CurrentPattern.SetStepIndex(index);
            }

            public void Reset()
            {
                CurrentPatternBar = 0;
                CurrentPatternIndex = GetProgressionPatternIndex(CurrentPatternBar, Patterns, PatternProgressionType, ref Random);
                CurrentPattern = Patterns[CurrentPatternIndex];
                CurrentPattern.SetStepIndex(0);
            }
        }

        public Unmanaged ToUnmanaged()
        {
            if (patterns.Count == 0 || patterns == null)
                patterns = new List<AnysongPattern>(1);
            var unmanagedPatterns = new AnysongPattern.Unmanaged[patterns.Count];
            for (int i = 0; i < patterns.Count; i++)
            {
                unmanagedPatterns[i] = patterns[i].ToUnmanaged();
            }

            if (_currentPattern == null)
                _currentPattern = patterns[0];
            
            return new Unmanaged
            {
                PatternProgressionType = patternProgressionType,
                Patterns = new NativeArray<AnysongPattern.Unmanaged>(unmanagedPatterns, Allocator.Persistent),
                CurrentPattern = _currentPattern.ToUnmanaged(),
                CurrentPatternBar = _currentPatternBar,
                CurrentPatternIndex = _currentPatternIndex,
                Random = new Unity.Mathematics.Random((uint)Random.Range(1, int.MaxValue))
            };
        }

        public void Init(AnysongTrackSettings songSongTrackSettings)
        {
            patterns = new List<AnysongPattern>(1);

            var pattern = new AnysongPattern();
            pattern.Init();
            patterns.Add(pattern);
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
            //_currentPatternBar++;
            //_currentPatternIndex = GetProgressionPatternIndex(_currentPatternBar);
            //_currentPattern = GetPattern(_currentPatternIndex);
            //_currentPattern.SetStepIndex(0);
        }

        static int GetProgressionPatternIndex(int patternBar, NativeArray<AnysongPattern.Unmanaged> patterns,
            PatternProgressionType patternProgressionType, ref Unity.Mathematics.Random random)
        {
            int patternIndex = (int)Mathf.Repeat(patternBar, patterns.Length);
            switch (patternProgressionType)
            {
                case PatternProgressionType.Sequence:

                    break;
                case PatternProgressionType.WeightedRandom:
                    bool didFindPattern = false;
                    float totalWeight = 0;

                    // First pass: calculate total weight
                    for (var i = 0; i < patterns.Length; i++)
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
                        float randomValue = random.NextFloat(0f, totalWeight); // <-- replaces Random.Range

                        float currentWeight = 0;

                        // Second pass: find the selected pattern
                        for (var i = 0; i < patterns.Length; i++)
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
                    patternIndex = random.NextInt(0, patterns.Length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return patternIndex;
        }


        public void Reset()
        {
            //_currentPatternBar = 0;
            //if (patterns.Count > 0)
            //{
            //    _currentPatternIndex = GetProgressionPatternIndex(_currentPatternBar);
            //    _currentPattern = GetPattern(_currentPatternIndex);
            //}
//
            //foreach (var pattern in patterns)
            //{
            //    pattern.Reset();
            //}
        }

        public void SyncToClock()
        {
            Debug.LogWarning("SyncToClock not implemented for AnysongSectionTrack");
            //_currentPatternIndex = GetProgressionPatternIndex(AnywhenMetronome.Instance.CurrentBar);
            //_currentPattern = GetPattern(_currentPatternIndex);
            //_currentPattern.SyncToClock();
        }

        public void SetTrack(AnysongTrackSettings songTrack)
        {
            Debug.LogWarning("SetTrack not implemented for AnysongSectionTrack");
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