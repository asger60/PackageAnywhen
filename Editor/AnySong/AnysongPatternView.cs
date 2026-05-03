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
        private static VisualElement _patternControlsElement;
        static int _currentPatternNoteIndex;
        static bool _polyfonic;
        public static bool IsPolyfonic => _polyfonic;
        static AnysongPatternStep _currentSelectedPatternStep;
        private static int _currentStepIndex;
        static bool _isPercussionTrack;
        static bool _isMonoVoice;

        public enum EditModes
        {
            NotePitch,
            NoteVelocity,
            NoteLength,
            NoteChance,
            NoteWeights
        }

        static EditModes _currentEditMode;
        public static EditModes CurrentEditMode => _currentEditMode;
        static AnysongPatternStep _movePatternStep;
        static AnysongPatternStep _preMovePatternStepCopy;

        public static void Clear()
        {
            _stepButtonsHolders.Clear();
            _parent = null;
            _stepViewToStep.Clear();
            _stepViewCullumns.Clear();
            _allStepButtons.Clear();
            _currentHoverStepButton = null;
            _patternControlsElement = null;
        }

        public static void Draw(VisualElement parent)
        {
            //_currentSelectedPatternStep = null;
            _parent = parent;
            _patternControlsElement = _parent.parent.Q<VisualElement>("PatternControls");
            _patternControlsElement.Clear();
            _patternControlsElement.Add(AnysongPatternControls.Draw());


            _stepButtonsHolders.Clear();
            _stepViewCullumns.Clear();
            _allStepButtons.Clear();
            _stepViewToStep.Clear();

            if (_parent != null)
            {
                _parent.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                _parent.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            }

            _parent = parent;
            _parent.Clear();

            var currentSelectionCurrentSection = AnysongEditorWindow.CurrentSelection.CurrentSection;
            if (currentSelectionCurrentSection.IsNull()) return;
            if (AnysongEditorWindow.CurrentSelection.CurrentSectionTrack.IsNull())
            {
                foreach (var t in currentSelectionCurrentSection.tracks)
                {
                    var trackElement = new VisualElement();
                    trackElement.AddToClassList("pattern-track-element");

                    var track = t;
                    trackElement.Add(DrawPatternSteps(track, true));
                    _parent.Add(trackElement);
                }
            }
            else
            {
                var trackElement = new VisualElement();
                var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
                trackElement.Add(DrawPatternSteps(track, false));
                _parent.Add(trackElement);
            }

            _currentPatternNoteIndex = 0;
            _isPercussionTrack =
                AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.instrument is AnywhenSampleInstrument
                    sampleInstrument
                && sampleInstrument.clipSelectType == AnywhenSampleInstrument.ClipSelectType.Percussion;
            if (_isPercussionTrack)
                SetPolyfonic(true);
            _isMonoVoice = AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.voices == 1;


            AddCallbacks();
        }

        static void AddCallbacks()
        {
            _parent.focusable = true;
            _parent.pickingMode = PickingMode.Position;
            _parent.Focus();
            _parent.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _parent.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _parent.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        private static void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            _parent.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _parent.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _parent.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        public static void SetEditMode(EditModes editMode)
        {
            _currentEditMode = editMode;
            Refresh();
        }

        public static void SetPolyfonic(bool polyfonic)
        {
            _polyfonic = polyfonic;
            Refresh();
        }

        public static void OffsetPatternNoteIndex(int direction)
        {
            direction = Mathf.Clamp(direction, -1, 1);
            _currentPatternNoteIndex += direction;
            Refresh();
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
            OffsetPatternNoteIndex((int)Mathf.Clamp(evt.delta.y, -1, 1));
        }

        private static void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.X)
            {
                if (!_currentSelectedPatternStep.IsNull())
                {
                    DeleteStep(_currentSelectedPatternStep);
                }
            }


            if (evt.keyCode == KeyCode.C)
            {
                if (!_currentSelectedPatternStep.IsNull())
                {
                    _stepCopy = _currentSelectedPatternStep.Clone();
                    //CopyStep(_currentSelectedPatternStep);
                }
            }

            if (evt.keyCode == KeyCode.V && !_stepCopy.IsNull())
            {
                PasteStep(_stepCopy, _currentHoverStepButton.PatternStepIndex);
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (!_currentSelectedPatternStep.IsNull()) _currentSelectedPatternStep.ShiftUp();
                Refresh();
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (!_currentSelectedPatternStep.IsNull()) _currentSelectedPatternStep.ShiftDown();
                Refresh();
            }

            if (evt.keyCode == KeyCode.RightArrow || evt.keyCode == KeyCode.LeftArrow)
            {
                if (!_currentSelectedPatternStep.IsNull())
                {
                    if (_movePatternStep.IsNull()) _movePatternStep = _currentSelectedPatternStep.Clone();
                    _currentSelectedPatternStep.Init();
                    if (!_preMovePatternStepCopy.IsNull())
                    {
                        PasteStep(_preMovePatternStepCopy, _currentStepIndex);
                    }

                    if (evt.keyCode == KeyCode.RightArrow) _currentStepIndex++;
                    else _currentStepIndex--;

                    _currentStepIndex = (int)Mathf.Repeat(_currentStepIndex, 16);

                    MoveStep(_movePatternStep, _currentStepIndex);

                    _movePatternStep = default;
                }
            }
        }


        private static void DeleteStep(AnysongPatternStep patternStep)
        {
            patternStep.Init();
            Refresh();
        }


        static void MoveStep(AnysongPatternStep copy, int stepIndex)
        {
            _preMovePatternStepCopy = AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex];
            AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex] = copy;
            _currentSelectedPatternStep = AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex];
            Refresh();
        }


        static void PasteStep(AnysongPatternStep copy, int stepIndex)
        {
            AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex] = copy.Clone();
            _currentSelectedPatternStep = AnysongEditorWindow.CurrentSelection.CurrentPattern.steps[stepIndex];
            Refresh();
        }

        private static VisualElement DrawPatternSteps(AnysongSectionTrack currentSectionTrack, bool compact)
        {
            Debug.Log("drawPatternSteps");
            int patternIndex = AnysongEditorWindow.CurrentSelection.CurrentPatternIndex;

            if (patternIndex > currentSectionTrack.patterns.Count - 1) patternIndex = 0;
            var stepButtonsHolder = new VisualElement
            {
                name = "StepButtonsHolder"
            };
            stepButtonsHolder.AddToClassList("pattern-steps-holder");
            int rowCount = 1;
            if (!compact) rowCount = 15;
            int noteStartIndex = _currentPatternNoteIndex;
            if (AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.instrument is AnywhenSampleInstrument
                sampleInstrument)
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
                    name = "StepButtonsRow"
                };
                rowElement.AddToClassList("pattern-step-row");
                string text = "";
                if (AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.instrument is AnywhenSampleInstrument
                    {
                        clipSelectType: AnywhenSampleInstrument.ClipSelectType.Percussion
                    })
                {
                    text = AnywhenSampleInstrument.MidiDrumMappings[rowIndex].Name;
                }
                else
                {
                    if ((rowIndex + noteStartIndex) % 7 == 0) text = "C" + (rowIndex + noteStartIndex) / 7;
                }

                Label rowLabel = new Label
                {
                    text = text
                };
                rowLabel.AddToClassList("pattern-row-label");
                rowElement.Add(rowLabel);
                if (currentSectionTrack.patterns.Count > 0)
                {
                    for (int stepIndex = 0; stepIndex < 16; stepIndex++)
                    {
                        var thisStep = currentSectionTrack.patterns[patternIndex].steps[stepIndex];
                        var stepButton = new AnysongPatternStepButton(rowElement, thisStep, stepIndex, noteStartIndex + rowIndex,
                            _polyfonic, noteStartIndex, noteStartIndex + rowCount);

                        _stepViewToStep.Add(stepButton, thisStep);
                        if (!_stepViewCullumns.TryGetValue(stepIndex, out var stepViews))
                        {
                            stepViews = new List<AnysongPatternStepButton>();
                            _stepViewCullumns[stepIndex] = stepViews;
                        }

                        stepViews.Add(stepButton);
                        _allStepButtons.Add(stepButton);
                    }
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
            foreach (var anysongStepView in _allStepButtons)
            {
                anysongStepView.SetHighLighted(false);
            }


            if (_stepViewCullumns.TryGetValue(AnysongEditorWindow.GetPlaybackStepIndexForCurrent(), out var stepViews))
            {
                foreach (var stepView in stepViews)
                {
                    stepView.SetHighLighted(state);
                }
            }
        }


        public static void Refresh()
        {
            if (_parent == null) return;

            _stepButtonsHolders.Clear();
            _stepViewCullumns.Clear();
            _allStepButtons.Clear();
            _stepViewToStep.Clear();
            if (_parent != null)
            {
                _parent.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                _parent.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            }

            _parent.Clear();
            var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
            _parent.Add(DrawPatternSteps(track, false));
            AddCallbacks();
        }


        public static void SelectStep(AnysongPatternStep patternStep)
        {
            _currentSelectedPatternStep = patternStep;
        }


        public static void SetStepIndex(int stepIndex)
        {
            _currentStepIndex = stepIndex;
        }
    }
}