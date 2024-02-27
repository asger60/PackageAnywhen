using System;
using System.Collections.Generic;
using Anywhen.Composing;

[Serializable]
public class AnySectionTrack
{
    public List<AnyPattern> patterns;
    public AnySongTrack anySongTrack;

    public int currentEditPatternIndex;


    
    
    public void Init(AnySongTrack songSongTrack)
    {
        anySongTrack = songSongTrack;
        patterns = new List<AnyPattern> { new() };
        foreach (var pattern in patterns)
        {
            pattern.Init();
        }
    }

    public AnySectionTrack Clone()
    {
        var clone = new AnySectionTrack
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

    public AnySongTrack GetSongTrack()
    {
        return anySongTrack;
    }

    
}