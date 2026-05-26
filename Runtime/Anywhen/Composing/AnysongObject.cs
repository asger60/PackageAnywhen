using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "Anysong", menuName = "Anywhen/Anysong", order = 1)]
    public class AnysongObject : ScriptableObject
    {
        [Range(10, 200)] public int tempo;
        [Range(0, 1f)] public float songVolume = 1;
        
        public List<AnysongSection> Sections;
        public List<AnysongTrackSettings> Tracks;
        
        public AnywhenSnapshot snapshotA = new();
        public AnywhenSnapshot snapshotB = new();
        
        public string author = "Floppy Club";


        [ContextMenu("ClearPatterns")]
        void ClearPatterns()
        {
            Debug.Log("ClearPatterns"); 
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
                        for (var i = 0; i < pattern.steps.Count; i++)
                        {
                            var step = pattern.steps[i];
                            var patternStep = step;
                            //patternStep.mixWeight = Random.Range(0, 1f);
                            pattern.steps[i] = patternStep;
                        }
                    }
                }
            }
        }
    }
}