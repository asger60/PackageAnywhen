using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [CreateAssetMenu(fileName = "PercussiveJamObject", menuName = "Anywhen/JamMode/Percussion")]

    public class JamModePercussion : JamModeBase
    {
        public AnywhenInstrument[] instrumentObjects;
    }
}
