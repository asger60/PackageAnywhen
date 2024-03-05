using System.Collections.Generic;
using Anywhen.Composing;
using UnityEngine;


[CreateAssetMenu(fileName = "Anysong", menuName = "Anywhen/Anysong", order = 1)]
public class AnysongObject : ScriptableObject
{
    public int tempo;
    public List<AnysongSection> Sections;
    public List<AnysongTrack> Tracks;
    public string coverGraphic;
    public Color playerBackgroundColor;
    
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
}