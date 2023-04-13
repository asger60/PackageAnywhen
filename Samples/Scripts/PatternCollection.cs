using System.Collections;
using System.Collections.Generic;
using Anywhen;
using Samples.Scripts;
using UnityEngine;


[CreateAssetMenu(fileName = "New Pattern Collection", menuName = "Anywhen/Sample/PatternCollection", order = 51)]

public class PatternCollection : ScriptableObject
{
    public DrumPatternMixer.Pattern[] patterns;
    public AnywhenMetronome.TickRate tickRate;
}
