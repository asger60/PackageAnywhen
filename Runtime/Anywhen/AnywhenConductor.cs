using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    public class AnywhenConductor : MonoBehaviour
    {
        public int _rootNote = 0;
        public AnywhenScaleObject anywhenScale;
        private AnywhenScaleObject _currentAnywhenScale;

        private static AnywhenConductor _instance;

        public static AnywhenConductor Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("Conductor is missing, please add to scene");
                }

                return _instance;
            }
        }

        private AnywhenProgressionPatternObject _currentAnywhenProgressionPattern;
        private int _currentPatternStep;
        private bool _scaleOverridden;
        private bool _rootOverridden;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            AnywhenMetronome.Instance.OnNextBar += OnNextBar;
            _scaleOverridden = false;
            if (anywhenScale != null)
                _currentAnywhenScale = anywhenScale;
        }

        private void OnNextBar()
        {
            if (!_rootOverridden)
                _rootNote = _currentAnywhenProgressionPattern.patternSteps[_currentPatternStep].rootNote;

            if (!_scaleOverridden)
                _currentAnywhenScale = _currentAnywhenProgressionPattern.patternSteps[_currentPatternStep].anywhenScale;

            _currentPatternStep++;
            _currentPatternStep =
                (int)Mathf.Repeat(_currentPatternStep, _currentAnywhenProgressionPattern.patternSteps.Length);
            //if (_currentPatternStep >= _currentPattern.patternSteps.Length-1)
            //    _currentPatternStep = 0;
        }


        public int GetScaledNote(int noteStep)
        {
            if (_currentAnywhenScale == null) return 0;
            if (_currentAnywhenScale.notes == null || _currentAnywhenScale.notes.Length == 0) return 0;
            int octave = (noteStep / _currentAnywhenScale.notes.Length) * 12;
            return _currentAnywhenScale.notes[(int)Mathf.Repeat(noteStep, _currentAnywhenScale.notes.Length)] + octave +
                   _rootNote;
        }

        public void OverrideScale(AnywhenScaleObject newAnywhenScale)
        {
            _currentAnywhenScale = newAnywhenScale;
            _scaleOverridden = true;
        }

        public void OverridePattern(AnywhenProgressionPatternObject anywhenProgressionPatternObject)
        {
            _currentAnywhenProgressionPattern = anywhenProgressionPatternObject;
            _currentPatternStep = 0;
        }

        public void OverrideRootNote(int newRoot)
        {
            _rootNote = newRoot;
            _rootOverridden = true;
        }
    }
}