using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenConductor : MonoBehaviour
    {
        [SerializeField] int _rootNote = 0;
        [SerializeField] AnywhenScaleObject anywhenScale;
        AnywhenScaleObject.Unmanaged _currentAnywhenScale;

        public static AnywhenConductor Instance => AnywhenRuntime.Conductor;

        public AnywhenProgressionPatternObject initialProgressionPattern;

        private AnywhenProgressionPatternObject _currentProgressionPattern;
        private int _currentPatternStep;
        private bool _scaleOverridden;
        private bool _rootOverridden;


        private void Start()
        {
            if (anywhenScale == null)
            {
                anywhenScale = Resources.Load<AnywhenScaleObject>("Scales/Minor");
            }


            _scaleOverridden = false;
            if (anywhenScale != null)
                _currentAnywhenScale = anywhenScale.ToUnmanaged();
            if (initialProgressionPattern != null)
                _currentProgressionPattern = initialProgressionPattern;


            //AnywhenMetronome.Instance.OnNextBar += OnNextBar;
        }

        private void OnNextBar()
        {
            if (_currentProgressionPattern)
            {
                _currentPatternStep++;
                _currentPatternStep =
                    (int)Mathf.Repeat(_currentPatternStep, _currentProgressionPattern.patternSteps.Length);

                if (!_rootOverridden)
                {
                    _rootNote = _currentProgressionPattern.patternSteps[_currentPatternStep].rootNote;
                }

                if (!_scaleOverridden)
                {
                    _currentAnywhenScale = _currentProgressionPattern.patternSteps[_currentPatternStep].anywhenScale.ToUnmanaged();
                }
            }
        }


        public int GetScaledNote(int noteStep, int maxNote = -1)
        {
            if (_currentAnywhenScale.IsNull())
            {
                Debug.LogError("No scale set!");
                return 0;
            }

            if (_currentAnywhenScale.IsNull() || _currentAnywhenScale.notes.Length == 0) return 0;
            int numNotes = _currentAnywhenScale.notes.Length;
            if (numNotes == 0) return 0;
            noteStep += _rootNote;

            int octave = Mathf.FloorToInt((float)noteStep / numNotes);
            int noteIndex = noteStep % numNotes;
            if (noteIndex < 0)
            {
                noteIndex += numNotes;
            }

            int returnNote = _currentAnywhenScale.notes[noteIndex] + (octave * 12);

            if (maxNote > 0 && returnNote >= maxNote)
            {
                returnNote -= 12;
            }

            return returnNote;
        }

        public int[] GetScaledNotes(int[] notes)
        {
            int[] returnNotes = new int[notes.Length];
            for (int i = 0; i < returnNotes.Length; i++)
            {
                returnNotes[i] = GetScaledNote(notes[i]);
            }

            return returnNotes;
        }

        public void OverrideScale(AnywhenScaleObject newAnywhenScale)
        {
            _currentAnywhenScale = newAnywhenScale.ToUnmanaged();
            _scaleOverridden = true;
        }

        public void OverridePattern(AnywhenProgressionPatternObject anywhenProgressionPatternObject)
        {
            _currentProgressionPattern = anywhenProgressionPatternObject;
            _currentPatternStep = 0;
        }

        public void OverrideRootNote(int newRoot)
        {
            _rootNote = newRoot;
            _rootOverridden = true;
        }

        public void SetScaleProgression(AnywhenProgressionPatternObject.ProgressionStep.Unmanaged step)
        {
            _currentAnywhenScale = step.AnywhenScale;
            _rootNote = step.RootNote;
        }

        public static AnywhenScaleObject GetDefaultScale()
        {
            return Resources.Load<AnywhenScaleObject>("Scales/Major");
        }

        public void SetDefaultScale()
        {
            Debug.Log("Setting default scale");
            _currentAnywhenScale = GetDefaultScale().ToUnmanaged();
        }
    }
}