using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    public class AnywhenConductor : MonoBehaviour
    {
        [FormerlySerializedAs("_rootNote")] [SerializeField]
        int initialRootNote = 0;

        [FormerlySerializedAs("anywhenScale")] [SerializeField]
        AnywhenScaleObject initialScale;

        static AnywhenConductor Instance => AnywhenRuntime.Conductor;


        AnywhenProgressionPatternObject.ProgressionStep.Unmanaged _currentBaseProgressionStep;
        AnywhenProgressionPatternObject _currentOverrideProgression;


        private void Start()
        {
            if (initialScale == null)
            {
                initialScale = Resources.Load<AnywhenScaleObject>("Scales/Minor");
            }

            _currentBaseProgressionStep = new AnywhenProgressionPatternObject.ProgressionStep.Unmanaged
            {
                AnywhenScale = initialScale.ToUnmanaged(),
                RootNote = initialRootNote
            };
        }


        public static int GetScaledNote(int noteStep)
        {
            if (Instance._currentBaseProgressionStep.IsNull)
                return 0;

            if (Instance._currentOverrideProgression)
            {
                return Instance.GetScaledNote(Instance._currentOverrideProgression.GetCurrentUnmanagedStep(), noteStep);
            }

            return Instance.GetScaledNote(Instance._currentBaseProgressionStep, noteStep);
        }

        int GetScaledNote(AnywhenProgressionPatternObject.ProgressionStep.Unmanaged progressionStep, int noteStep)
        {
            if (progressionStep.AnywhenScale.IsNull() || progressionStep.AnywhenScale.notes.Length == 0)
            {
                Debug.LogError("No scale set!");
                return 0;
            }

            int numNotes = progressionStep.AnywhenScale.notes.Length;
            int octave = Mathf.FloorToInt((float)noteStep / numNotes);
            int noteIndex = noteStep % numNotes;
            if (noteIndex < 0)
            {
                noteIndex += numNotes;
            }

            int returnNote = progressionStep.AnywhenScale.notes[noteIndex] + (octave * 12);
            return returnNote + progressionStep.RootNote;
        }


        public static void SetBaseScaleProgressionStep(AnywhenProgressionPatternObject.ProgressionStep.Unmanaged progressionStep)
        {
            Instance._currentBaseProgressionStep = progressionStep;
        }

        public static void SetOverrideScaleProgression(AnywhenProgressionPatternObject overrideProgression)
        {
            Instance._currentOverrideProgression = overrideProgression;
        }

        public static AnywhenScaleObject GetDefaultScale()
        {
            return Resources.Load<AnywhenScaleObject>("Scales/Major");
        }
    }
}