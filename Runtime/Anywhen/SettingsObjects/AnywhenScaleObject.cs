using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/Conductor/ScalesObject")]

    
    public class AnywhenScaleObject : ScriptableObject
    {
        [NonReorderable]
        public int[] notes;
    }
}
