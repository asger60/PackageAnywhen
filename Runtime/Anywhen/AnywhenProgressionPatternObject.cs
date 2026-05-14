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
                public int RootNote;
                public AnywhenScaleObject.Unmanaged AnywhenScale;
                public bool IsNull => AnywhenScale.IsNull();
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    RootNote = rootNote,
                    AnywhenScale = anywhenScale.ToUnmanaged()
                };
            }
        }


        public ProgressionStep[] patternSteps;

        public struct Unmanaged
        {
            public NativeArray<ProgressionStep.Unmanaged> patternSteps;

            public bool IsNull()
            {
                return !patternSteps.IsCreated || patternSteps.Length == 0;
            }
        }

        public Unmanaged ToUnmanaged()
        {
            var unmanagedSteps = new NativeArray<ProgressionStep.Unmanaged>(patternSteps.Length, Allocator.Persistent);
            for (int i = 0; i < patternSteps.Length; i++)
            {
                unmanagedSteps[i] = patternSteps[i].ToUnmanaged();
            }

            return new Unmanaged
            {
                patternSteps = unmanagedSteps
            };
        }
    }
}