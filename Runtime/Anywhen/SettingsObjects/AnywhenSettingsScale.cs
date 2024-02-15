using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/AudioObjects/ScalesObject")]

    public class AnywhenSettingsScale : AnywhenInstrument
    {
        public AnywhenScaleObject[] scales;
        public int[] rootNotes;
    }
}
