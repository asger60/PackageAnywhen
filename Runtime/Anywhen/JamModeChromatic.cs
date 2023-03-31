using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    
    [CreateAssetMenu(fileName = "ChromaticJamObject", menuName = "Rytmos/JamMode/Chromatic")]

    public class JamModeChromatic : JamModeBase
    {
        [FormerlySerializedAs("instrument")] [FormerlySerializedAs("instrumentObject")] public AnywhenInstrument anywhenInstrument;
        
        
        public int[] notes;
        public int[] notesController;
    }
}
