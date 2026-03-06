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
        private static Dictionary<AnysongStepView, AnysongPatternStep> _stepViewToStep = new();
        static Dictionary<int, List<AnysongStepView>> _stepViewCullumns = new();
        private static List<AnysongStepView> _allStepButtons = new();

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
                    trackElement.Add(DrawPatternSteps(track, i, true));
                    parent.Add(trackElement);
                }
            }
            else
            {
                var trackElement = new VisualElement();
                var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
                trackElement.Add(DrawPatternSteps(track, AnysongEditorWindow.CurrentSelection.CurrentTrackIndex, false));
                parent.Add(trackElement);
            }
        }


        public static void RefreshPatterns()
        {
            Debug.Log("Refreshing pattern steps " + _stepButtonsHolders.Count);
        }


        private static VisualElement DrawPatternSteps(AnysongSectionTrack currentSectionTrack, int patternIndex, bool compact)
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

                    var stepButton = new AnysongStepView(rowElement, thisStep, stepIndex, noteStartIndex + rowIndex);
                    _stepViewToStep.Add(stepButton, thisStep);

                    if (!_stepViewCullumns.TryGetValue(stepIndex, out var stepViews))
                    {
                        stepViews = new List<AnysongStepView>();
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

        public static void Refresh()
        {
            _parent.Clear();
            var trackElement = new VisualElement();
            var track = AnysongEditorWindow.CurrentSelection.CurrentSectionTrack;
            trackElement.Add(DrawPatternSteps(track, AnysongEditorWindow.CurrentSelection.CurrentTrackIndex, false));
            _parent.Add(trackElement);
        }
    }
}