using System;
using System.Collections.Generic;
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

        public PatternProgressionType patternProgressionType = PatternProgressionType.WeightedRandom;


        public List<AnysongPattern> patterns;
        private int _selectedTrackPatternIndex; // this should probably storred in a different place

        private AnysongPattern _currentPattern;
        private int _currentPatternBar;
        int _currentPatternIndex;
        public AnysongTrackSettings anysongTrackSettings;
        
        
        public void Init(AnysongTrackSettings songSongTrackSettings)
        {
            Debug.Log("Initing track " + songSongTrackSettings.trackType);
            anysongTrackSettings = songSongTrackSettings;
            _selectedTrackPatternIndex = 0;
            patterns = new List<AnysongPattern> { new() };
            foreach (var pattern in patterns)
            {
                pattern.Init();
            }
        }

        public AnysongSectionTrack Clone()
        {
            var clone = new AnysongSectionTrack
            {
                patterns = new List<AnysongPattern>()
            };
            for (var i = 0; i < patterns.Count; i++)
            {
                clone.patterns.Add(patterns[i].Clone());
            }


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
                var newPattern = new AnysongPattern();
                newPattern.Init();
                patterns.Add(newPattern);
            }

            return _currentPattern ??= patterns[0];
        }

        public int GetPlayingPatternIndex()
        {
            return _currentPatternIndex;
        }

        public int GetSelectedPatternIndex()
        {
            return _selectedTrackPatternIndex;
        }


        public void SetSelectedPattern(int index)
        {
            _selectedTrackPatternIndex = index;
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
            anysongTrackSettings = songTrack;
        }
    }
}