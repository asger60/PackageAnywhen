using System;
using System.Collections.Generic;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UIElements;


public static class AnysongProgressionsView
{
    private static List<VisualElement> _patternButtonsHolders = new List<VisualElement>();

    public static void Draw(VisualElement parent, AnysongObject currentSong)
    {
        parent.Clear();
        _patternButtonsHolders.Clear();


        for (var i = 0; i < currentSong.Tracks.Count; i++)
        {
            var trackElement = new VisualElement
            {
                style =
                {
                    height = 45,
                    flexDirection = FlexDirection.Row,
                    //width = 60
                }
            };
            var button = new Button
            {
                name = "ProgressionButton",
                tooltip = 0 + "-" + i + "-" + 0,
                text = "P",
                style =
                {
                    height = 40,
                    width = 20
                }
            };
            button.AddToClassList("progression-edit-button");

            trackElement.Add(button);
            var track = AnysongEditorWindow.CurrentSelection.CurrentSection.tracks[i];
            trackElement.Add(DrawTrackPattern(parent, i, track.GetSelectedPatternIndex()));
            parent.Add(trackElement);
        }
    }

    public static void HilightPattern(int trackIndex, int currentPatternIndex, int currentSelectionIndex)
    {
        if (trackIndex > _patternButtonsHolders.Count - 1) return;
        _patternButtonsHolders[trackIndex].Query<Button>("PatternButton").ForEach(button =>
        {
            var str = button.tooltip.Split("-");
            int thisIndex = Int32.Parse(str[2]);

            if (thisIndex == currentPatternIndex)
            {
                button.AddToClassList("triggered");
            }
            else
            {
                button.RemoveFromClassList("triggered");
            }
        });
    }

    public static void ResetTriggered()
    {
        foreach (var patternButtonHolder in _patternButtonsHolders)
        {
            patternButtonHolder.Query<Button>("PatternButton").ForEach(button => { button.RemoveFromClassList("triggered"); });
        }
    }


    private static VisualElement DrawTrackPattern(VisualElement parent, int trackIndex, int selectedPattern)
    {
        var patternsButtonHolder = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };

        _patternButtonsHolders.Add(patternsButtonHolder);

        var thisTrack = AnysongEditorWindow.CurrentSelection.CurrentSection.tracks[trackIndex];

        for (var patternIndex = 0; patternIndex < thisTrack.patterns.Count; patternIndex++)
        {
            var button = new Button
            {
                name = "PatternButton",
                tooltip = 0 + "-" + trackIndex + "-" + patternIndex,
                text = patternIndex.ToString(),
                style =
                {
                    width = 30,
                    height = 40,
                }
            };
            button.AddToClassList("progression-select-button");
            if (patternIndex == selectedPattern)
                button.AddToClassList("editing");
            else
                button.RemoveFromClassList("editing");

            patternsButtonHolder.Add(button);
        }

        var addRemoveContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                width = 20,
                height = 40,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
            }
        };


        var addButton = new Button
        {
            name = "AddButton",
            text = "+",
            tooltip = trackIndex.ToString(),
            style =
            {
                width = 20,
                height = 40
            }
        };
        addButton.AddToClassList("progression-add-button");
        addButton.style.visibility = thisTrack.patterns.Count >= 4 ? Visibility.Hidden : Visibility.Visible;

        addRemoveContainer.Add(addButton);
        patternsButtonHolder.Add(addRemoveContainer);
        return patternsButtonHolder;
    }

    public static void SetPatternIndexForTrack(int trackIndex, int patternIndex)
    {
        AnysongEditorWindow.GetCurrentSelection().CurrentSection.tracks[trackIndex].SetSelectedPattern(patternIndex);

        _patternButtonsHolders[trackIndex].Query<Button>("PatternButton").ForEach(button =>
        {
            var str = button.tooltip.Split("-");
            int thisPatternIndex = Int32.Parse((string)str[2]);

            if (thisPatternIndex == patternIndex)
                button.AddToClassList("editing");
            else
                button.RemoveFromClassList("editing");
        });
    }
}