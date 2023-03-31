using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/AudioObjects/ScalesObject")]

    public class AnywhenSettingsScale : AnywhenSettingsBase
    {
        public AnywhenScaleObject[] scales;
        public int[] rootNotes;
    }
}
