using PackageAnywhen.Runtime.Anywhen;
using UnityEngine;
using UnityEngine.Serialization;

namespace Rytmos.AudioSystem
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/AudioObjects/ScalesObject")]

    public class AnywhenSettingsScale : AnywhenSettingsBase
    {
        public AnywhenScaleObject[] scales;
        public int[] rootNotes;
    }
}
