using System;
using System.Collections.Generic;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongSequencesView
    {
        private static List<VisualElement> _stepButtonsHolders = new List<VisualElement>();
        private static List<VisualElement> _patternButtonsHolders = new List<VisualElement>();

        public static void Draw(VisualElement parent, AnysongSection currentSection)
        {
            _stepButtonsHolders.Clear();
            _patternButtonsHolders.Clear();
            parent.Clear();
            parent.Add(new Label("Sequences"));
            Debug.Log("draw sequences");
            if (currentSection != null)
            {
                for (var i = 0; i < currentSection.tracks.Count; i++)
                {
                    var track = currentSection.tracks[i];
                    DrawTrackPattern(parent, i, 0);
                    DrawPatternSteps(parent, track, i, 0);
                }
            }
        }

        public static void SetPatternIndexForTrack(int trackIndex, int patternIndex)
        {
            _patternButtonsHolders[trackIndex].Query<Button>("PatternButton").ForEach(button =>
            {
                var str = button.tooltip.Split("-");
                int thisPatternIndex = Int32.Parse(str[2]);

                button.style.backgroundColor = thisPatternIndex == patternIndex
                    ? AnysongEditorWindow.ColorGreyDark
                    : AnysongEditorWindow.ColorGreyDefault;
            });


            int i = 0;
            _stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
            {
                button.tooltip = i + "-" + trackIndex + "-" + patternIndex;
                i++;
            });
        }

        public static void RefreshPatterns()
        {
            for (var i = 0; i < AnysongEditorWindow.CurrentSong.Sections[0].tracks.Count; i++)
            {
                RefreshPatternSteps(i, 0);
            }
        }

        private static void DrawTrackPattern(VisualElement parent, int trackIndex, int selectedPattern)
        {
            //var addButton = new Button
            //{
            //    name = "AddButton",
            //    tooltip = trackIndex.ToString(),
            //    text = "+"
            //};
            //var removeButton = new Button
            //{
            //    name = "RemoveButton",
            //    tooltip = trackIndex.ToString(),
            //    text = "-"
            //};
            var patternsButtonHolder = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };

            _patternButtonsHolders.Add(patternsButtonHolder);
//
            //patternsButtonHolder.Add(addButton);
            //patternsButtonHolder.Add(removeButton);
            var thisTrack = AnysongEditorWindow.CurrentSong.Sections[0].tracks[trackIndex];


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
                        backgroundColor =
                            patternIndex == selectedPattern
                                ? AnysongEditorWindow.ColorGreyDark
                                : AnysongEditorWindow.ColorGreyDefault,
                    }
                };
                patternsButtonHolder.Add(button);
            }

            parent.Add(patternsButtonHolder);
        }

        private static void DrawPatternSteps(VisualElement parent, AnysongSectionTrack currentSectionTrack,
            int trackIndex,
            int patternIndex)
        {
            //if (currentSectionTrack?.EditorCurrentPattern == null) return;

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
            _stepButtonsHolders.Add(stepButtonsHolder);

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


        private static void RefreshPatternSteps(int trackIndex, int patternIndex)
        {
            foreach (var stepButtonHolder in _stepButtonsHolders)
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

            return !thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 : AnysongEditorWindow.ColorGreyAccent;
        }

        static Color GetBackgroundColorFromButtonState(AnyPatternStep thisStep)
        {
            if (thisStep.noteOn && thisStep.IsChord) return AnysongEditorWindow.ColorHilight3;

            return thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 :
                thisStep.noteOff ? AnysongEditorWindow.ColorHilight2 : AnysongEditorWindow.ColorGreyAccent;
        }


        public static void HilightStepIndex(int trackIndex, int stepIndex)
        {
            _stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
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