using Anywhen.Attributes;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class JamModeBase : AnywhenSettingsBase
    {
        [Space(10)]
        [Range(0,1f)] public float quantization = 1;

        public AnywhenMetronome.TickRate quantizeRate = AnywhenMetronome.TickRate.Sub16;
        public int loopLength = 1;
        [MinMaxSlider(0,1)]
        public Vector2 volumeRange = new Vector2(0.65f,0.9f);


        
    }
}
