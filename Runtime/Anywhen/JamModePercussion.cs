using PackageAnywhen.Runtime.Anywhen;
using PackageAnywhen.Runtime.Anywhen.AudioSystem;
using Rytmos.AudioSystem;
using UnityEngine;

namespace Rytmos.JamMode.JamModeObjectTypes
{
    [CreateAssetMenu(fileName = "PercussiveJamObject", menuName = "Anywhen/JamMode/Percussion")]

    public class JamModePercussion : JamModeBase
    {
        public AnywhenInstrument[] instrumentObjects;
    }
}
