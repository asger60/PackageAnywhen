using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongSequencesView
    {
        public static void Draw(VisualElement parent, AnySection currentSection)
        {
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
                    text = "P"+i,
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

            var stepsButtonHolder = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            parent.Add(stepsButtonHolder);

            for (int stepIndex = 0; stepIndex < currentSectionTrack.patterns[0].steps.Count; stepIndex++)
            {
                var thisStep = currentSectionTrack.patterns[0].steps[stepIndex];

                var button = new Button
                {
                    name = "StepButton",
                    tooltip = stepIndex + "-" + trackIndex + "-" + patternIndex,

                    text = thisStep.notes.Count == 0 ? "0" : thisStep.notes[0].ToString(),

                    style =
                    {
                        minWidth = 50,
                        color = !thisStep.noteOn ? AnysongEditorWindow.ColorHilight1 : AnysongEditorWindow.ColorGrey,
                        backgroundColor = thisStep.noteOn
                            ? AnysongEditorWindow.ColorHilight1
                            : thisStep.noteOff
                                ? AnysongEditorWindow.ColorHilight2
                                : AnysongEditorWindow.ColorGrey,
                    }
                };
                stepsButtonHolder.Add(button);
            }
        }
    }
}