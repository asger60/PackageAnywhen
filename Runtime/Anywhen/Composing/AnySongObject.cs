using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "AnySong", menuName = "Anywhen/AnySong", order = 1)]

public class AnySongObject : ScriptableObject
{
    public int tempo;
    public List<AnySection> Sections;

    [ContextMenu("Init")]
    void Init()
    {
        foreach (var section in Sections)
        {
            section.Init();
        }
    }
}