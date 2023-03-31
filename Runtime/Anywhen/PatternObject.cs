using UnityEngine;

namespace Rytmos.AudioSystem
{
    [CreateAssetMenu(fileName = "New progression pattern", menuName = "Anywhen/Conductor/Note Progression")]

    public class PatternObject : ScriptableObject
    {
        [System.Serializable]
        public class PatternStep
        {
            public int rootNote;
            public AnywhenScaleObject anywhenScale;
        }

        
        public PatternStep[] patternSteps;

    }
}
