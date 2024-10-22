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


        public List<AnyPattern> patterns;

        
        public bool isMuted;
        public bool isSolo;
        private int _selectedTrackPatternIndex;


        public void Init(AnysongTrack songSongTrack)
        {
            _selectedTrackPatternIndex = 0;
            patterns = new List<AnyPattern> { new() };
            foreach (var pattern in patterns)
            {
                pattern.Init();
            }
        }

        public AnysongSectionTrack Clone()
        {
            var clone = new AnysongSectionTrack
            {
                patterns = new List<AnyPattern>()
            };
            for (var i = 0; i < patterns.Count; i++)
            {
                clone.patterns.Add(patterns[i].Clone());
            }


            return clone;
        }

        public AnyPattern GetPattern(int index)
        {
            if (index >= patterns.Count)
                return patterns[0];

            return patterns[index];
        }

        public AnyPattern GetPlayingPattern()
        {
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


        private AnyPattern _currentPattern;
        private int _currentPatternBar;
        private int _currentPatternIndex;

        public void AdvancePlayingPattern()
        {
            _currentPatternBar++;
            switch (patternProgressionType)
            {
                case PatternProgressionType.Sequence:
                    _currentPatternIndex = (int)Mathf.Repeat(_currentPatternBar, patterns.Count);
                    break;
                case PatternProgressionType.WeightedRandom:
                    bool didFindPattern = false;
                    float maxRandom = 100;
                    for (var i = 0; i < patterns.Count; i++)
                    {
                        var anyPattern = patterns[i];
                        float thisTriggerChance = anyPattern.triggerChances[(int)Mathf.Repeat(_currentPatternBar, 4)];
                        float thisRnd = Random.Range(0, maxRandom);
                        if (thisTriggerChance > thisRnd)
                        {
                            _currentPatternIndex = i;
                            //_currentPattern = anyPattern;
                            didFindPattern = true;
                            break;
                        }

                        maxRandom -= thisTriggerChance;
                    }

                    if (!didFindPattern)
                    {
                        _currentPatternIndex = 0;
                    }

                    break;
                case PatternProgressionType.Random:
                    _currentPatternIndex = Random.Range(0, patterns.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentPattern = patterns[_currentPatternIndex];
        }


        public void Reset()
        {
            _currentPatternBar = 0;
            if (patterns.Count > 0)
                _currentPattern = patterns[0];
            foreach (var pattern in patterns)
            {
                pattern.Reset();
            }
        }
    }
}