using System;
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
        bool _polyfonic;
        private int _gridMax, _gridMin;

        public AnysongPatternStepButton(VisualElement parentElement, AnysongPatternStep patternStep, int stepIndex, int noteIndex, bool polyfonic,
            int gridMin, int gridMax)
        {
            _button = new Button
            {
                name = "StepButton"
            };
            _polyfonic = polyfonic;
            _patternStep = patternStep;
            _noteIndex = noteIndex;
            _stepIndex = stepIndex;
            _gridMin = gridMin;
            _gridMax = gridMax;
            if (stepIndex % 4 == 0)
                _button.AddToClassList("pattern-step-fourth");
            if (noteIndex % 7 == 0)
                _button.AddToClassList("pattern-step-fourth");


            _button.AddToClassList("pattern-step-button");
            parentElement.Add(_button);

            Refresh();
            _button.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerEnterEvent>(OnPointerEnterEvent, TrickleDown.TrickleDown);
            _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent, TrickleDown.TrickleDown);
            _button.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            _button.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            _button.UnregisterCallback<PointerEnterEvent>(OnPointerEnterEvent, TrickleDown.TrickleDown);
            _button.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent, TrickleDown.TrickleDown);
            _button.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
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
            AnysongEditorWindow.SelectPatternStep(_patternStep, _stepIndex);

            if (evt.button != 0)
            {
                return;
            }

            switch (AnysongPatternView.CurrentEditMode)
            {
                case AnysongPatternView.EditModes.NotePitch:
                    if (_patternStep.chordNotes.Contains((_patternStep.rootNote * -1) + _noteIndex))
                    {
                        if (_polyfonic)
                            _patternStep.chordNotes.Remove(_noteIndex - _patternStep.rootNote);
                        else
                            _patternStep.chordNotes.Clear();
                    }
                    else
                    {
                        if (_patternStep.NoteOn)
                        {
                            if (_polyfonic)
                            {
                                _patternStep.chordNotes.Add((_patternStep.rootNote * -1) + _noteIndex);
                            }
                            else
                            {
                                _patternStep.chordNotes.Clear();
                                _patternStep.rootNote = _noteIndex;
                                _patternStep.chordNotes.Add(0);
                            }
                        }
                        else
                        {
                            _patternStep.rootNote = _noteIndex;
                            if (_polyfonic)
                            {
                                _patternStep.chordNotes.Add(0);
                            }
                            else
                            {
                                _patternStep.chordNotes.Clear();
                                _patternStep.chordNotes.Add(0);
                            }
                        }
                    }

                    break;
                case AnysongPatternView.EditModes.NoteVelocity:
                    _patternStep.velocity = Mathf.InverseLerp(_gridMin, _gridMax, _noteIndex + 1);

                    break;
                case AnysongPatternView.EditModes.NoteLength:
                    _patternStep.duration = Mathf.InverseLerp(_gridMin, _gridMax, _noteIndex + 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void Refresh()
        {
            _button.RemoveFromClassList("pattern-step-note-mono");
            _button.RemoveFromClassList("pattern-step-note-poly");

            switch (AnysongPatternView.CurrentEditMode)
            {
                case AnysongPatternView.EditModes.NotePitch:
                    if (_patternStep.NoteOn)
                    {
                        if (_patternStep.chordNotes.Contains((_patternStep.rootNote * -1) + _noteIndex))
                        {
                            _button.AddToClassList(_patternStep.IsChord ? "pattern-step-note-poly" : "pattern-step-note-mono");
                        }
                    }

                    break;
                case AnysongPatternView.EditModes.NoteVelocity:
                    if (_patternStep.NoteOn && _patternStep.velocity > Mathf.InverseLerp(_gridMin, _gridMax , _noteIndex))
                    {
                        _button.AddToClassList("pattern-step-note-mono");
                    }

                    break;
                case AnysongPatternView.EditModes.NoteLength:
                    if (_patternStep.NoteOn && _patternStep.duration > Mathf.InverseLerp(_gridMin, _gridMax , _noteIndex))
                    {
                        _button.AddToClassList("pattern-step-note-mono");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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