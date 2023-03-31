using System;
using PackageAnywhen.Runtime.Anywhen.AudioSystem;
using UnityEngine;
namespace Rytmos.JamMode.JamModeObjectTypes
{
    [CreateAssetMenu(fileName = "FaderJamObject", menuName = "Rytmos/JamMode/Fader")]

    public class JamModeEffect : JamModeBase
    {
        
        [Space(10)] 
        public string[] effectParameterNames;
        public float[] effectParameterRestValues;
        
        [Serializable]
        public struct EffectSettings
        {
            [NonReorderable]public float[] effectValues;
        }


        [NonReorderable]public EffectSettings[] effectPresetSettings;

        
    }
}
