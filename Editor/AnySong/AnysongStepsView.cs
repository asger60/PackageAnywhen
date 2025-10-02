using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UIElements;


public static class AnysongStepsView
{
    private static List<VisualElement> _stepButtonsHolders = new List<VisualElement>();
    private static List<VisualElement> _patternButtonsHolders = new List<VisualElement>();

    public static void Draw(VisualElement parent)
    {
        _stepButtonsHolders.Clear();
        _patternButtonsHolders.Clear();
        parent.Clear();
        var spacer = new VisualElement
        {
            style =
            {
                height = 1
            }
        };

        if (AnysongEditorWindow.CurrentSelection.CurrentSection == null) return;

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
            trackElement.Add(DrawPatternSteps(parent, track, i, track.GetSelectedPatternIndex()));
            parent.Add(trackElement);
        }
    }


    public static void RefreshPatterns()
    {
        RefreshPatternSteps();
    }


    private static VisualElement DrawPatternSteps(VisualElement parent, AnysongSectionTrack currentSectionTrack, int trackIndex, int patternIndex)
    {
        patternIndex = Mathf.Min(patternIndex, currentSectionTrack.patterns.Count - 1);
        var stepButtonsHolder = new VisualElement
        {
            name = "StepButtonsHolder",
            style =
            {
                alignItems = Align.Center,
                flexDirection = FlexDirection.Row
            }
        };
        _stepButtonsHolders.Add(stepButtonsHolder);


        for (int stepIndex = 0; stepIndex < 16; stepIndex++)
        {
            if (currentSectionTrack.patterns[patternIndex] == null || currentSectionTrack.patterns[patternIndex].steps.Count == 0) continue;
            var thisStep = currentSectionTrack.patterns[patternIndex].steps[stepIndex];

            var button = new Button
            {
                name = "StepButton",
                tooltip = stepIndex + "-" + trackIndex + "-" + patternIndex,

                text = thisStep.rootNote.ToString(),

                //style =
                //{
                //    color = GetTextColorFromButtonState(thisStep),
                //    backgroundColor = stepIndex > currentSectionTrack.patterns[patternIndex].patternLength
                //        ? Color.clear
                //        : GetBackgroundColorFromButtonState(thisStep)
                //}
            };
            if (thisStep.noteOn && !thisStep.IsChord)
                button.AddToClassList("pattern-step-note-mono");

            if (thisStep.noteOn && thisStep.IsChord)
                button.AddToClassList("pattern-step-note-poly");

            button.AddToClassList("pattern-step-button");
            stepButtonsHolder.Add(button);
        }

        return stepButtonsHolder;
    }


    private static void RefreshPatternSteps()
    {
        var selection = AnysongEditorWindow.GetCurrentSelection();

        foreach (var stepButtonHolder in _stepButtonsHolders)
        {
            int index = 0;
            stepButtonHolder.Query<Button>("StepButton").ForEach(button =>
            {
                var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
                button.text = thisStep.rootNote.ToString();
                //button.style.backgroundColor = GetBackgroundColorFromButtonState(thisStep);
                //button.style.color = GetTextColorFromButtonState(thisStep);
                button.RemoveFromClassList("pattern-step-note-mono");
                button.RemoveFromClassList("pattern-step-note-poly");
                if (thisStep.noteOn && !thisStep.IsChord)
                    button.AddToClassList("pattern-step-note-mono");

                if (thisStep.noteOn && thisStep.IsChord)
                    button.AddToClassList("pattern-step-note-poly");

                var str = button.tooltip.Split("-");
                int buttonTrackIndex =
                    Int32.Parse(str[1]); // todo - maybe figure out a better way to retrieve the index
                int buttonPatternIndex =
                    Int32.Parse(str[2]); // todo - maybe figure out a better way to retrieve the index
                index++;
                var pattern = selection.CurrentSection.tracks[buttonTrackIndex].patterns[buttonPatternIndex];

                button.SetEnabled(index <= pattern.patternLength);
            });
        }
    }

    public static void ResetTriggered()
    {
        foreach (var stepButtonHolder in _stepButtonsHolders)
        {
            stepButtonHolder.Query<Button>("StepButton").ForEach(button =>
            {
                stepButtonHolder.RemoveFromClassList("triggered");
            });
        }
    }

    public static void HilightStepIndex(int trackIndex, bool state)
    {
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
        AnysongEditorWindow.GetCurrentSelection().CurrentSection.tracks[trackIndex].SetSelectedPattern(patternIndex);


        int i = 0;
        _stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
        {
            button.tooltip = i + "-" + trackIndex + "-" + patternIndex;
            i++;
        });
    }
}