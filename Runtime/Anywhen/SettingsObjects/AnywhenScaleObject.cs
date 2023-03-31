using UnityEngine;

namespace Rytmos.AudioSystem
{
    [CreateAssetMenu(fileName = "New scale object", menuName = "Anywhen/Conductor/ScalesObject")]

    
    public class AnywhenScaleObject : ScriptableObject
    {
        [NonReorderable]
        public int[] notes;
    }
}
