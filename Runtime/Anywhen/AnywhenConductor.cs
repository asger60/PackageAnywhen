﻿using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenConductor : MonoBehaviour
    {
        public int _rootNote = 0;
        public AnywhenScaleObject anywhenScale;
        private AnywhenScaleObject _currentAnywhenScale;


        public static AnywhenConductor Instance => AnywhenRuntime.Conductor;
        
        public AnywhenProgressionPatternObject initialProgressionPattern;

        private AnywhenProgressionPatternObject _currentProgressionPattern;
        private int _currentPatternStep;
        private bool _scaleOverridden;
        private bool _rootOverridden;



        private void Start()
        {
            _scaleOverridden = false;
            if (anywhenScale != null)
                _currentAnywhenScale = anywhenScale;
            if (initialProgressionPattern != null)
                _currentProgressionPattern = initialProgressionPattern;
            AnywhenMetronome.Instance.OnNextBar += OnNextBar;

        }

        private void OnNextBar()
        {
            if (!_rootOverridden)
                _rootNote = _currentProgressionPattern.patternSteps[_currentPatternStep].rootNote;

            if (!_scaleOverridden)
                _currentAnywhenScale = _currentProgressionPattern.patternSteps[_currentPatternStep].anywhenScale;

            _currentPatternStep++;
            _currentPatternStep =
                (int)Mathf.Repeat(_currentPatternStep, _currentProgressionPattern.patternSteps.Length);
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
            _currentProgressionPattern = anywhenProgressionPatternObject;
            _currentPatternStep = 0;
        }

        public void OverrideRootNote(int newRoot)
        {
            _rootNote = newRoot;
            _rootOverridden = true;
        }
    }
}