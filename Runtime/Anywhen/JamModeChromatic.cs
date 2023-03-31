using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Runtime.Anywhen.AudioSystem;
using Rytmos.AudioSystem;
using Rytmos.AudioSystem.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Rytmos.JamMode.JamModeObjectTypes
{
    
    [CreateAssetMenu(fileName = "ChromaticJamObject", menuName = "Rytmos/JamMode/Chromatic")]

    public class JamModeChromatic : JamModeBase
    {
        [FormerlySerializedAs("instrument")] [FormerlySerializedAs("instrumentObject")] public AnywhenInstrument anywhenInstrument;
        
        
        public int[] notes;
        public int[] notesController;
    }
}
