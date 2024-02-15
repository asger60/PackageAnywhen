using System;
using System.Collections.Generic;
using UnityEditor;

[Serializable]
public class AnySectionTrack
{
    public List<AnyPattern> patterns;

    public AnySongTrack anySongTrack;

    
    
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

    

#if UNITY_EDITOR
    public void DrawInspector()
    {
        var track = this;
        anySongTrack.instrument = (AnywhenInstrument)EditorGUILayout.ObjectField("Instrument",
            anySongTrack.instrument,
            typeof(AnywhenInstrument));
        anySongTrack.volume = EditorGUILayout.FloatField("Volume", anySongTrack.volume);
    }
#endif
}