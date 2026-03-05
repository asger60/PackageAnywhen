using System;
using System.Collections.Generic;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public static class AnysongStepsView
    {
        private static List<VisualElement> _stepButtonsHolders = new();
        private static VisualElement _parent;

        public static void Draw(VisualElement parent)
        {
            Debug.Log("Drawing steps");
            _parent = parent;
            _stepButtonsHolders.Clear();
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
                    trackElement.Add(DrawPatternSteps(track, i, track.GetSelectedPatternIndex(), true));
                    parent.Add(trackElement);
                }
            }
            else
            {
                var trackElement = new VisualElement();
                var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
                trackElement.Add(DrawPatternSteps(track, AnysongEditorWindow.CurrentSelection.CurrentTrackIndex, track.GetSelectedPatternIndex(),
                    false));
                parent.Add(trackElement);
            }
        }


        public static void RefreshStep()
        {
            int rowIndex = AnysongEditorWindow.CurrentSelection.CurrentRowIndex;
            int currentButtonIndex = AnysongEditorWindow.CurrentSelection.CurrentStepIndex;
            var button = _parent.Query<VisualElement>("StepButtonsRow").ToList()[rowIndex].Query<Button>("StepButton").ToList()[currentButtonIndex];


            var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
            button.text = AnysongEditorWindow.TrackEdit ? "" : thisStep.rootNote.ToString();
            button.RemoveFromClassList("pattern-step-note-mono");
            button.RemoveFromClassList("pattern-step-note-poly");
            if (thisStep.noteOn && !thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                button.AddToClassList("pattern-step-note-mono");

            if (thisStep.noteOn && thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                button.AddToClassList("pattern-step-note-poly");
            
        }

        public static void RefreshPatterns()
        {
            Debug.Log("Refreshing pattern steps " + _stepButtonsHolders.Count);

            for (var i = 0; i < _stepButtonsHolders.Count; i++)
            {
                var stepButtonHolder = _stepButtonsHolders[i];
                int index = 0;
                var rowIndex = i;
                Debug.Log(rowIndex);
                stepButtonHolder.Q("StepButtonsRow").Query<Button>("StepButton").ForEach(button =>
                {
                    var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
                    button.text = AnysongEditorWindow.TrackEdit ? "" : thisStep.rootNote.ToString();

                    button.RemoveFromClassList("pattern-step-note-mono");
                    button.RemoveFromClassList("pattern-step-note-poly");
                    if (thisStep.noteOn && !thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                        button.AddToClassList("pattern-step-note-mono");

                    if (thisStep.noteOn && thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                        button.AddToClassList("pattern-step-note-poly");


                    button.SetEnabled(index <= AnysongEditorWindow.CurrentSelection.CurrentPattern.patternLength);
                });
            }
        }


        private static VisualElement DrawPatternSteps(AnysongSectionTrack currentSectionTrack, int trackIndex, int patternIndex, bool compact)
        {
            patternIndex = Mathf.Min(patternIndex, currentSectionTrack.patterns.Count - 1);
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
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                VisualElement row = new VisualElement()
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
                    var button = new Button
                    {
                        name = "StepButton",
                        tooltip = stepIndex + "-" + trackIndex + "-" + patternIndex + "-" + rowIndex,
                        text = compact ? thisStep.rootNote.ToString() : "",
                    };

                    if (stepIndex % 4 == 0)
                        button.AddToClassList("pattern-step-fourth");

                    if (thisStep.noteOn && !thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                    {
                        button.AddToClassList("pattern-step-note-mono");
                    }

                    if (thisStep.noteOn && thisStep.IsChord && thisStep.rootNote == rowIndex + 36)
                    {
                        button.AddToClassList("pattern-step-note-poly");
                    }

                    button.AddToClassList("pattern-step-button");
                    row.Add(button);
                }

                stepButtonsHolder.Add(row);
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
            if (AnysongEditorWindow.TrackEdit) return;

            var pattern = AnysongEditorWindow.GetCurrentPlayingPatternForTrack(trackIndex);

            _stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
            {
                var str = button.tooltip.Split("-");
                int buttonStep = Int32.Parse(str[0]); // todo - maybe figure out a better way to retrieve the index

                if (state && buttonStep == pattern.InternalIndex)
                    button.AddToClassList("triggered");
                else
                    button.RemoveFromClassList("triggered");
            });
        }

        public static void SetPatternIndexForTrack(int trackIndex, int patternIndex)
        {
            Debug.LogWarning("SetPatternIndexForTrack not implemented yet");
            //AnysongEditorWindow.GetCurrentSelection().CurrentSection.tracks[trackIndex].SetSelectedPattern(patternIndex);
//
            //int i = 0;
            //_stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
            //{
            //    button.tooltip = i + "-" + trackIndex + "-" + patternIndex;
            //    i++;
            //});
        }
    }
}