using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public class AnysongPatternStepButton
    {
        Button _button;
        AnysongPatternStep _patternStep;
        public AnysongPatternStep PatternStep => _patternStep;
        public int PatternStepIndex => _stepIndex;

        int _noteIndex;
        private int _stepIndex;

        public AnysongPatternStepButton(VisualElement parentElement, AnysongPatternStep patternStep, int stepIndex, int noteIndex)
        {
            _button = new Button
            {
                name = "StepButton"
            };
            _patternStep = patternStep;
            _noteIndex = noteIndex;
            _stepIndex = stepIndex;
            if (stepIndex % 4 == 0)
                _button.AddToClassList("pattern-step-fourth");

            _button.AddToClassList("pattern-step-button");
            parentElement.Add(_button);

            Refresh();
            _button.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerEnterEvent>(OnPointerEnterEvent, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent, TrickleDown.TrickleDown);
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            AnysongPatternView.OnHoverExit(this);
        }

        private void OnPointerEnterEvent(PointerEnterEvent evt)
        {
            AnysongPatternView.OnHoverEnter(this);
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                AnysongEditorWindow.SelectPatternStep(_patternStep, _stepIndex);
                return;
            }

            if (_patternStep.chordNotes.Contains((_patternStep.rootNote * -1) + _noteIndex))
            {
                _patternStep.chordNotes.Remove(_noteIndex - _patternStep.rootNote);
            }
            else
            {
                if (_patternStep.NoteOn)
                {
                    _patternStep.chordNotes.Add((_patternStep.rootNote * -1) + _noteIndex);
                }
                else
                {
                    _patternStep.rootNote = _noteIndex;
                    _patternStep.chordNotes.Add(0);
                }
            }


            AnysongEditorWindow.SelectPatternStep(_patternStep, _stepIndex);
        }


        private void Refresh()
        {
            _button.RemoveFromClassList("pattern-step-note-mono");
            _button.RemoveFromClassList("pattern-step-note-poly");

            if (_patternStep.NoteOn)
            {
                if (_patternStep.chordNotes.Contains((_patternStep.rootNote * -1) + _noteIndex))
                {
                    _button.AddToClassList(_patternStep.IsChord ? "pattern-step-note-poly" : "pattern-step-note-mono");
                }
            }
        }

        public void SetHighLighted(bool state)
        {
            if (state)
                _button.AddToClassList("triggered");
            else
                _button.RemoveFromClassList("triggered");
        }
    }
}