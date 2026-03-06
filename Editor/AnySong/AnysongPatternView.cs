using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public static class AnysongPatternView
    {
        private static List<VisualElement> _stepButtonsHolders = new();
        private static VisualElement _parent;
        private static Dictionary<AnysongPatternStepButton, AnysongPatternStep> _stepViewToStep = new();
        static Dictionary<int, List<AnysongPatternStepButton>> _stepViewCullumns = new();
        private static List<AnysongPatternStepButton> _allStepButtons = new();
        private static AnysongPatternStepButton _currentHoverStepButton;
        private static AnysongPatternStep _stepCopy;

        public static void Draw(VisualElement parent)
        {
            _parent = parent;
            _stepButtonsHolders.Clear();
            _stepViewCullumns.Clear();
            _allStepButtons.Clear();
            parent.Clear();

            if (AnysongEditorWindow.CurrentSelection.CurrentSection == null) return;
            if (AnysongEditorWindow.CurrentSelection.CurrentSectionTrack == null)
            {
                for (var i = 0; i < AnysongEditorWindow.CurrentSelection.CurrentSection.tracks.Count; i++)
                {
                    var trackElement = new VisualElement
                    {
                        style =
                        {
                            height = 45,
                        }
                    };

                    var track = AnysongEditorWindow.CurrentSelection.CurrentSection.tracks[i];
                    trackElement.Add(DrawPatternSteps(track, true));
                    parent.Add(trackElement);
                }
            }
            else
            {
                var trackElement = new VisualElement();
                var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
                trackElement.Add(DrawPatternSteps(track, false));
                parent.Add(trackElement);
            }

            _parent.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _parent.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _parent.focusable = true;
            _parent.pickingMode = PickingMode.Position;
            _parent.Focus();
        }

        public static void OnHoverEnter(AnysongPatternStepButton stepButton)
        {
            _currentHoverStepButton = stepButton;
        }

        public static void OnHoverExit(AnysongPatternStepButton stepButton)
        {
            _currentHoverStepButton = null;
        }

        private static void OnWheel(WheelEvent evt)
        {
            if (_currentHoverStepButton == null) return;
            _currentHoverStepButton.PatternStep.rootNote += (int)Mathf.Clamp(evt.delta.y, -1, 1);
            Refresh();
        }

        private static void OnKeyDown(KeyDownEvent evt)
        {
            if (_currentHoverStepButton == null) return;

            if (evt.keyCode == KeyCode.X)
            {
                if (_currentHoverStepButton.PatternStep != null)
                {
                    DeleteStep(_currentHoverStepButton.PatternStep);
                }
            }


            if (evt.keyCode == KeyCode.C)
            {
                if (_currentHoverStepButton.PatternStep != null)
                {
                    CopyStep(_currentHoverStepButton.PatternStep);
                }
            }

            if (evt.keyCode == KeyCode.V && _stepCopy != null)
            {
                PasteStep(_currentHoverStepButton.PatternStepIndex);
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (_currentHoverStepButton.PatternStep != null) _currentHoverStepButton.PatternStep.rootNote++;
                Refresh();
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (_currentHoverStepButton.PatternStep != null) _currentHoverStepButton.PatternStep.rootNote--;
                Refresh();
            }
        }

        private static void DeleteStep(AnysongPatternStep patternStep)
        {
            patternStep.rootNote = 0;
            patternStep.chordNotes.Clear();
            Refresh();
        }

        static void CopyStep(AnysongPatternStep step)
        {
            _stepCopy = step.Clone();
        }

        static void PasteStep(int stepIndex)
        {
            AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex] = _stepCopy;
            Refresh();
            _stepCopy = _stepCopy.Clone();
        }

        private static VisualElement DrawPatternSteps(AnysongSectionTrack currentSectionTrack, bool compact)
        {
            int patternIndex = AnysongEditorWindow.CurrentSelection.CurrentPatternIndex;
            if (patternIndex > currentSectionTrack.patterns.Count - 1) patternIndex = 0;
            var stepButtonsHolder = new VisualElement
            {
                name = "StepButtonsHolder",
                style =
                {
                    alignItems = Align.Center,
                }
            };
            int rowCount = 1;
            if (!compact) rowCount = 8;
            int noteStartIndex = 0;
            if (AnysongEditorWindow.CurrentSelection.CurrentSongTrack.instrument is AnywhenSampleInstrument sampleInstrument)
            {
                if (sampleInstrument.clipSelectType == AnywhenSampleInstrument.ClipSelectType.Percussion)
                {
                    noteStartIndex = 35;
                    rowCount = 13;
                }
            }

            for (int rowIndex = rowCount - 1; rowIndex >= 0; rowIndex--)
            {
                VisualElement rowElement = new VisualElement()
                {
                    name = "StepButtonsRow",
                    style =
                    {
                        width = new StyleLength(new Length(100, LengthUnit.Percent)),
                        alignItems = Align.Center,
                        flexDirection = FlexDirection.Row,
                        height = 45,
                    }
                };
                for (int stepIndex = 0; stepIndex < 16; stepIndex++)
                {
                    if (currentSectionTrack.patterns[patternIndex] == null || currentSectionTrack.patterns[patternIndex].steps.Count == 0) continue;
                    var thisStep = currentSectionTrack.patterns[patternIndex].steps[stepIndex];

                    var stepButton = new AnysongPatternStepButton(rowElement, thisStep, stepIndex, noteStartIndex + rowIndex);
                    _stepViewToStep.Add(stepButton, thisStep);

                    if (!_stepViewCullumns.TryGetValue(stepIndex, out var stepViews))
                    {
                        stepViews = new List<AnysongPatternStepButton>();
                        _stepViewCullumns[stepIndex] = stepViews;
                    }

                    stepViews.Add(stepButton);
                    _allStepButtons.Add(stepButton);
                }


                stepButtonsHolder.Add(rowElement);
                _stepButtonsHolders.Add(stepButtonsHolder);
            }


            return stepButtonsHolder;
        }


        public static void ResetTriggered()
        {
            foreach (var stepButtonHolder in _stepButtonsHolders)
            {
                stepButtonHolder.Query<Button>("StepButton").ForEach(button => { button.RemoveFromClassList("triggered"); });
            }
        }

        public static void HilightStepIndex(int trackIndex, bool state)
        {
            var pattern = AnysongEditorWindow.CurrentSelection.CurrentPattern;
            foreach (var anysongStepView in _allStepButtons)
            {
                anysongStepView.SetHighLighted(false);
            }


            if (_stepViewCullumns.TryGetValue(pattern.InternalIndex, out var stepViews))
            {
                foreach (var stepView in stepViews)
                {
                    stepView.SetHighLighted(true);
                }
            }
        }


        public static void Refresh()
        {
            _parent.Clear();
            var trackElement = new VisualElement();
            var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
            trackElement.Add(DrawPatternSteps(track, false));
            _parent.Add(trackElement);
        }
    }
}