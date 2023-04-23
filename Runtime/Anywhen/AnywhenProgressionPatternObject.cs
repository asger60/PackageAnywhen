using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [CreateAssetMenu(fileName = "New progression pattern", menuName = "Anywhen/Conductor/Note Progression")]

    public class AnywhenProgressionPatternObject : ScriptableObject
    {
        [System.Serializable]
        public class ProgressionStep
        {
            public int rootNote;
            public AnywhenScaleObject anywhenScale;
        }

        
        public ProgressionStep[] patternSteps;

    }
}
