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
            parent.Add(new Label("Sequences"));
            var spacer = new VisualElement
            {
                style =
                {
                    height = 1
                }
            };
            parent.Add(spacer);
            if (AnysongEditorWindow.CurrentSelection.CurrentSection != null)
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
                    trackElement.Add(DrawTrackPattern(parent, i, track.GetSelectedPatternIndex()));
                    trackElement.Add(DrawPatternSteps(parent, track, i, track.GetSelectedPatternIndex()));
                    parent.Add(trackElement);
                }
            }
        }

        public static void SetPatternIndexForTrack(int trackIndex, int patternIndex)
        {
            AnysongEditorWindow.GetCurrentSelection().CurrentSection.tracks[trackIndex]
                .SetSelectedPattern(patternIndex);
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

        public static void HilightPattern(int trackIndex, int currentPatternIndex, int currentSelectionIndex)
        {
            if (trackIndex > _patternButtonsHolders.Count - 1) return;
            _patternButtonsHolders[trackIndex].Query<Button>("PatternButton").ForEach(button =>
            {
                var str = button.tooltip.Split("-");
                int thisIndex = Int32.Parse(str[2]);
                if (thisIndex == currentPatternIndex)
                {
                    button.style.backgroundColor = AnysongEditorWindow.ColorHilight4;
                    button.style.color = AnysongEditorWindow.ColorGreyDark;
                }
                else
                {
                    button.style.color = Color.white;
                    button.style.backgroundColor = thisIndex == currentSelectionIndex
                        ? AnysongEditorWindow.ColorGreyDark
                        : AnysongEditorWindow.ColorGreyDefault;
                }
            });
        }

        public static void RefreshPatterns()
        {
            RefreshPatternSteps();
        }

        private static VisualElement DrawTrackPattern(VisualElement parent, int trackIndex, int selectedPattern)
        {
            var patternsButtonHolder = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
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
                        backgroundColor =
                            patternIndex == selectedPattern
                                ? AnysongEditorWindow.ColorGreyDark
                                : AnysongEditorWindow.ColorGreyDefault,
                    }
                };
                patternsButtonHolder.Add(button);
            }

            patternsButtonHolder.Add(AnysongEditorWindow.CreateAddRemoveButtons());
            patternsButtonHolder.Q<Button>("AddButton").tooltip = trackIndex.ToString();
            var deleteButton = patternsButtonHolder.Q<Button>("RemoveButton");
            deleteButton.tooltip = trackIndex.ToString();
            deleteButton.style.display = thisTrack.patterns.Count == 1 ? DisplayStyle.None : DisplayStyle.Flex;
            

            return patternsButtonHolder;
        }

        private static VisualElement DrawPatternSteps(VisualElement parent, AnysongSectionTrack currentSectionTrack,
            int trackIndex, int patternIndex)
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
            //parent.Add(stepButtonsHolder);
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

                    style =
                    {
                        minWidth = 50,
                        color = GetTextColorFromButtonState(thisStep),
                        backgroundColor = stepIndex > currentSectionTrack.patterns[patternIndex].patternLength
                            ? Color.clear
                            : GetBackgroundColorFromButtonState(thisStep)
                    }
                };
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
                    button.style.backgroundColor = GetBackgroundColorFromButtonState(thisStep);
                    button.style.color = GetTextColorFromButtonState(thisStep);

                    var str = button.tooltip.Split("-");
                    int buttonTrackIndex =
                        Int32.Parse(str[1]); // todo - maybe figure out a better way to retrieve the index
                    int buttonPatternIndex =
                        Int32.Parse(str[2]); // todo - maybe figure out a better way to retrieve the index
                    index++;
                    var pattern = selection.CurrentSection.tracks[buttonTrackIndex].patterns[buttonPatternIndex];

                    if (index > pattern.patternLength)
                    {
                        button.style.backgroundColor = Color.clear;
                    }
                });
            }
        }


        static Color GetTextColorFromButtonState(AnyPatternStep thisStep)
        {
            if (!thisStep.noteOn && !thisStep.noteOff) return AnysongEditorWindow.ColorGreyAccent;
            if (thisStep.noteOn && thisStep.IsChord) return AnysongEditorWindow.ColorGreyDark;

            return !thisStep.noteOn ? AnysongEditorWindow.ColorGreyDark : AnysongEditorWindow.ColorGreyAccent;
        }

        static Color GetBackgroundColorFromButtonState(AnyPatternStep thisStep)
        {
            if (!thisStep.noteOn && !thisStep.noteOff) return AnysongEditorWindow.ColorGreyAccent;
            if (thisStep.noteOn && thisStep.IsChord) return AnysongEditorWindow.ColorHilight3;

            return thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 :
                thisStep.noteOff ? AnysongEditorWindow.ColorHilight2 : AnysongEditorWindow.ColorGreyAccent;
        }


        public static void HilightStepIndex(int trackIndex, bool state)
        {
            int index = 0;
            var pattern = AnysongEditorWindow.GetCurrentPlayingPatternForTrack(trackIndex);

            _stepButtonsHolders[trackIndex].Query<Button>("StepButton").ForEach(button =>
            {
                var thisStep = AnysongEditorWindow.GetPatternStepFromTooltip(button.tooltip);
                var str = button.tooltip.Split("-");
                int buttonStep = Int32.Parse(str[0]); // todo - maybe figure out a better way to retrieve the index

                button.style.backgroundColor = (state && buttonStep == pattern.InternalIndex)
                    ? AnysongEditorWindow.ColorGreyDark
                    : GetBackgroundColorFromButtonState(thisStep);

                button.style.color = (state && buttonStep == pattern.InternalIndex)
                    ? GetBackgroundColorFromButtonState(thisStep)
                    : GetTextColorFromButtonState(thisStep);


                index++;


                //if (index > pattern.patternLength)
                //{
                //    button.style.backgroundColor = Color.clear;
                //}
            });
        }
    }
