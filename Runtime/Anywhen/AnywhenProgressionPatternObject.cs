using Anywhen.SettingsObjects;
using Unity.Collections;
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

            public ProgressionStep Clone()
            {
                var clone = new ProgressionStep
                {
                    rootNote = rootNote,
                    anywhenScale = anywhenScale
                };
                return clone;
            }

            public struct Unmanaged
            {
                public int rootNote;
                public AnywhenScaleObject.Unmanaged anywhenScale;
            }
            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    rootNote = rootNote,
                    //anywhenScale = anywhenScale.ToUnmanaged()
                };
            }
        }


        public ProgressionStep[] patternSteps;

    }
}