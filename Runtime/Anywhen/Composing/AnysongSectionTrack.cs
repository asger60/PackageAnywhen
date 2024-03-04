using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongSectionTrack
    {
        public List<AnyPattern> patterns;

        public AnimationCurve intensityMappingCurve;
    
    
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
            for (var i = 0; i < 16; i++)
            {
                clone.patterns.Add(patterns[i].Clone());
            }


            return clone;
        }


        public AnyPattern GetPattern(int currentBar)
        {
            var pattern = patterns[0];
            foreach (var anyPattern in patterns)
            {
                if (anyPattern.TriggerOnBar(currentBar)) pattern = anyPattern;
            }

            return pattern;
        }
    
    
    }
}