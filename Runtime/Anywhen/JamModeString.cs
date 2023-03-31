using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    [CreateAssetMenu(fileName = "StringJamObject", menuName = "Rytmos/JamMode/String")]

    public class JamModeString : JamModeBase
    {
        [FormerlySerializedAs("instrument")] [FormerlySerializedAs("instrumentObject")] public AnywhenInstrument anywhenInstrument;
        public int[] stringIndexes;
    }
}
