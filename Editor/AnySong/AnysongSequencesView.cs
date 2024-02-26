using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongSequencesView
    {
        private static List<VisualElement> _stepButtonsHolder = new List<VisualElement>();

        public static void Draw(VisualElement parent, AnySection currentSection)
        {
            _stepButtonsHolder.Clear();
            parent.Clear();
            parent.Add(new Label("Sequences"));
            Debug.Log("draw sequences");
            if (currentSection != null)
            {
                for (var i = 0; i < currentSection.tracks.Count; i++)
                {
                    var track = currentSection.tracks[i];
                    DrawTrackPattern(parent, track);
                    DrawPatternSteps(parent, track, i, 0);
                }
            }
        }

        public static void RefreshPatterns(AnySection currentSection)
        {
            if (currentSection != null)
            {
                for (var i = 0; i < currentSection.tracks.Count; i++)
                {
                    var track = currentSection.tracks[i];
                    RefreshPatternSteps(track, i, 0);
                }
            }
        }

        private static void DrawTrackPattern(VisualElement parent, AnySectionTrack track)
        {
            if (track == null) return;

            var addButton = new Button
            {
                text = "+"
            };
            var removeButton = new Button
            {
                text = "-"
            };
            var patternsButtonHolder = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            patternsButtonHolder.Add(addButton);
            patternsButtonHolder.Add(removeButton);

            for (var i = 0; i < track.patterns.Count; i++)
            {
                var button = new Button
                {
                    name = "PatternButton",
                    tabIndex = i,
                    text = "P" + i,
                    style =
                    {
                        width = 100,
                    }
                };
                patternsButtonHolder.Add(button);
            }

            parent.Add(patternsButtonHolder);
        }

        private static void DrawPatternSteps(VisualElement parent, AnySectionTrack currentSectionTrack, int trackIndex,
            int patternIndex)
        {
            if (currentSectionTrack?.EditorCurrentPattern == null) return;

            var stepButtonsHolder = new VisualElement
            {
                name = "StepButtonsHolder",
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            parent.Add(stepButtonsHolder);
            _stepButtonsHolder.Add(stepButtonsHolder);

            for (int stepIndex = 0; stepIndex < currentSectionTrack.patterns[0].steps.Count; stepIndex++)
            {
                var thisStep = currentSectionTrack.patterns[0].steps[stepIndex];

                var button = new Button
                {
                    name = "StepButton",
                    tooltip = stepIndex + "-" + trackIndex + "-" + patternIndex,

                    text = thisStep.rootNote.ToString(),

                    style =
                    {
                        minWidth = 50,
                        color = GetTextColorFromButtonState(thisStep),
                        backgroundColor = GetBackgroundColorFromButtonState(thisStep)
                    }
                };
                stepButtonsHolder.Add(button);
            }
        }


        private static void RefreshPatternSteps(AnySectionTrack currentSectionTrack, int trackIndex, int patternIndex)
        {
            if (currentSectionTrack?.EditorCurrentPattern == null) return;
            foreach (var stepButtonHolder in _stepButtonsHolder)
            {
                stepButtonHolder.Query<Button>("StepButton").ForEach(button =>
                {
                    var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
                    button.text = thisStep.rootNote.ToString();
                    button.style.backgroundColor = GetBackgroundColorFromButtonState(thisStep);
                    button.style.color = GetTextColorFromButtonState(thisStep);
                });
            }
        }


        static Color GetTextColorFromButtonState(AnyPatternStep thisStep)
        {
            if (thisStep.noteOn && thisStep.IsChord) return AnysongEditorWindow.ColorHilight1;

            return !thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 : AnysongEditorWindow.ColorGrey;
        }

        static Color GetBackgroundColorFromButtonState(AnyPatternStep thisStep)
        {
            if (thisStep.noteOn && thisStep.IsChord) return AnysongEditorWindow.ColorHilight3;

            return thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 :
                thisStep.noteOff ? AnysongEditorWindow.ColorHilight2 : AnysongEditorWindow.ColorGrey;
        }


        public static void HilightStepIndex(int trackIndex, int stepIndex)
        {
            _stepButtonsHolder[trackIndex].Query<Button>("StepButton").ForEach(button =>
            {
                var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
                var str = button.tooltip.Split("-");
                int buttonStep = Int32.Parse(str[0]); // todo - maybe figure out a better way to retrieve the index
                
                button.style.backgroundColor = buttonStep == stepIndex
                    ? Color.black
                    : GetBackgroundColorFromButtonState(thisStep);
                button.style.color = buttonStep == stepIndex
                    ? GetBackgroundColorFromButtonState(thisStep)
                    : GetTextColorFromButtonState(thisStep);
            });
        }
    }
}