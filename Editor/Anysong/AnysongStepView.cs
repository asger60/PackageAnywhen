using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public class AnysongStepView
    {
        Button _button;
        AnysongPatternStep _patternStep;
        int _noteIndex;

        public AnysongStepView(VisualElement parent, AnysongPatternStep patternStep, int stepIndex, int noteIndex)
        {
            _button = new Button();
            _patternStep = patternStep;
            _noteIndex = noteIndex;
            var stepIndex1 = stepIndex;
            if (stepIndex % 4 == 0)
                _button.AddToClassList("pattern-step-fourth");

            _button.AddToClassList("pattern-step-button");
            parent.Add(_button);


            _button.clicked += () =>
            {
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


                AnysongEditorWindow.SelectPatternStep(_patternStep, stepIndex1);
            };
            Refresh();
        }

        public void Refresh()
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
            if( state)
                _button.AddToClassList("triggered");
            else
                _button.RemoveFromClassList("triggered");
        }
    }
}