using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "Anysong", menuName = "Anywhen/Anysong", order = 1)]
    public class AnysongObject : ScriptableObject
    {
        [Range(10, 200)]
        public int tempo;
        public List<AnysongSection> Sections;
        public List<AnysongTrack> Tracks;
        [Range(0,1f)]
        public float songVolume = 1;
    
        [ContextMenu("Init")]
        void Init()
        {
            foreach (var section in Sections)
            {
                section.Init(Tracks);
            }
        }

        [ContextMenu("RandomizeStepWeights")]
        void RandomizeStepWeights()
        {
            foreach (var anySection in Sections)
            {
                foreach (var rSectionTrack in anySection.tracks)
                {
                    foreach (var pattern in rSectionTrack.patterns)
                    {
                        foreach (var step in pattern.steps)
                        {
                            step.mixWeight = Random.Range(0, 1f);
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            foreach (var track in Tracks)
            {
                track.Reset();
            }

            foreach (var section in Sections)
            {
                section.Reset();
            }
        }
    }
}