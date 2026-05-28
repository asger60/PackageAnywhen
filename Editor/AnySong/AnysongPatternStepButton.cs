using System;
using Anywhen;
using Anywhen.Composing;
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
        public AnysongPatternNote _patternNote;
        int _noteIndex;
        private int _stepIndex;
        bool _polyfonic;
        private int _gridMax, _gridMin;

        public AnysongPatternStepButton(
            VisualElement parentElement,
            AnysongPatternStep patternStep,
            int stepIndex, int noteIndex,
            bool polyfonic,
            int gridMin,
            int gridMax)
        {
            _button = new Button { name = "StepButton" };
            _polyfonic = polyfonic;
            _patternStep = patternStep;
            _noteIndex = noteIndex;
            _stepIndex = stepIndex;
            _gridMin = gridMin;
            _gridMax = gridMax;

            if (stepIndex % 4 == 0) _button.AddToClassList("pattern-step-fourth");
            if (noteIndex % 7 == 0) _button.AddToClassList("pattern-step-fourth");


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
            if (evt.button == 0)
            {
                var currentNote = _patternStep.GetNote(_noteIndex);
                if (!currentNote.IsNull())
                {
                    _patternStep.RemoveNote(currentNote);
                    AnysongPatternView.Refresh();   
                }
                else
                {
                    if (_patternStep.NoteOn)
                    {
                        if (!_polyfonic)
                        {
                            _patternStep.ClearNotes();
                        }

                        _patternStep.AddNote(new AnysongPatternNote(_noteIndex));
                    }
                    else
                    {
                        _patternStep.AddNote(new AnysongPatternNote(_noteIndex));
                    }

                    AnysongEditorWindow.SelectPatternStep(_patternStep, _stepIndex, _noteIndex);
                }

                AnysongEditorWindow.UpdateMidi(AnysongEditorWindow.CurrentSelection.CurrentSectionIndex,
                    AnysongEditorWindow.CurrentSelection.CurrentTrackIndex,
                    AnysongEditorWindow.CurrentSelection.CurrentPatternIndex);
            }


            if (evt.button == 1)
            {
                AnysongEditorWindow.SelectPatternStep(_patternStep, _stepIndex, _noteIndex);
                AnysongPatternView.SelectStep(_patternStep);
                AnysongPatternView.SetStepIndex(_stepIndex);
            }

            Refresh();
        }


        private void Refresh()
        {
            _button.RemoveFromClassList("pattern-step-note-mono");
            _button.RemoveFromClassList("pattern-step-note-poly");
            var thisNote = _patternStep.GetNote(_noteIndex);
            if (thisNote.IsNull() || !_patternStep.NoteOn) return;
            
            switch (AnysongPatternView.CurrentEditMode)
            {
                case AnysongPatternView.EditModes.NotePitch:
                    _button.style.backgroundColor = AnywhenColors.NoteVelocity;


                    break;
                case AnysongPatternView.EditModes.NoteVelocity:
                    _button.style.backgroundColor = AnywhenColors.GetNoteColor(AnywhenColors.NoteVelocity, thisNote.velocity);
                    break;
                case AnysongPatternView.EditModes.NoteLength:
                    _button.style.backgroundColor =
                        AnywhenColors.GetNoteColor(AnywhenColors.NoteLength, thisNote.duration / 0.5f);


                    break;
                case AnysongPatternView.EditModes.NoteChance:
                    _button.style.backgroundColor = AnywhenColors.GetNoteColor(AnywhenColors.NoteChance, thisNote.chance);


                    break;
                case AnysongPatternView.EditModes.NoteWeights:
                    _button.style.backgroundColor = AnywhenColors.GetNoteColor(AnywhenColors.NoteWeight, thisNote.mixWeight);

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