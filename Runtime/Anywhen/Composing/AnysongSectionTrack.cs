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

        public AnimationCurve intensityMappingCurve =
            new(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        public bool isMuted;
        public bool isSolo;


        public void Init(AnysongTrack songSongTrack)
        {
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

        private AnyPattern _currentPattern;

        public void AdvancePlayingPattern()
        {
            switch (patternProgressionType)
            {
                case PatternProgressionType.Sequence:
                    _currentPattern = patterns[(int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, patterns.Count)];
                    break;
                case PatternProgressionType.WeightedRandom:
                    bool didFindPattern = false;
                    float maxRandom = 100;
                    foreach (var anyPattern in patterns)
                    {
                        float thisTriggerChance = anyPattern.triggerChances[(int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, 4)];
                        float thisRnd = Random.Range(0, maxRandom);
                        Debug.Log(thisTriggerChance + " " + thisRnd);
                        if (thisTriggerChance > thisRnd)
                        {
                            _currentPattern = anyPattern;
                            didFindPattern = true;
                            Debug.Log("found a pattern");
                            break;
                        }

                        maxRandom -= thisTriggerChance;
                        //if (anyPattern.TriggerOnBar(AnywhenMetronome.Instance.CurrentBar)) _currentPattern = anyPattern;
                    }

                    if (!didFindPattern) 
                    {
                        Debug.Log("didn find a pattern");
                        _currentPattern = patterns[0];
                    }

                    break;
                case PatternProgressionType.Random:
                    _currentPattern = patterns[Random.Range(0, patterns.Count)];

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Reset()
        {
            _currentPattern = patterns[0];
            foreach (var pattern in patterns)
            {
                pattern.Reset();
            }
        }
    }
}