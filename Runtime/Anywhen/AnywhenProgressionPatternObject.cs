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

        public ProgressionStep.Unmanaged GetCurrentUnmanagedStep()
        {
            int index = AnywhenAudioMetronome.CurrentBar % patternSteps.Length;
            return patternSteps[index].ToUnmanaged();
        }
    }
}