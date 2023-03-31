using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Runtime.Anywhen.AudioSystem;
using Rytmos.AudioSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Rytmos.JamMode.JamModeObjectTypes
{
    [CreateAssetMenu(fileName = "StringJamObject", menuName = "Rytmos/JamMode/String")]

    public class JamModeString : JamModeBase
    {
        [FormerlySerializedAs("instrument")] [FormerlySerializedAs("instrumentObject")] public AnywhenInstrument anywhenInstrument;
        public int[] stringIndexes;
    }
}
