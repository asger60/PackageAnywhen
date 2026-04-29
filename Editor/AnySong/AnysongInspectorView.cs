using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Synth;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public static class AnysongInspectorView
    {
        private static VisualElement _parent;

        public static void Clear()
        {
            _parent = null;
        }

        public static void Draw(VisualElement parent)
        {
            _parent = parent;
        }

        static VisualElement Spacer()
        {
            var spacer = new VisualElement()
            {
                style = { minHeight = 10 }
            };
            return spacer;
        }


        public static void DrawSection(SerializedProperty section)
        {
            _parent.Clear();
            Draw(_parent);
            _parent.Add(Spacer());
            _parent.Add(CreatePropertyFieldWithCallback(section.FindPropertyRelative("sectionLength"), null));
            //_parent.Add(CreatePropertyFieldWithCallback(section.FindPropertyRelative("patternSteps"), null));
            _parent.Add(CreateUtilsBox());
        }

        public static void DrawPattern(SerializedProperty pattern, Action didUpdate)
        {
            _parent.Clear();
            Draw(_parent);

            _parent.Add(Spacer());


            _parent.Add(CreatePropertyFieldWithCallback(pattern.FindPropertyRelative("patternLength"), didUpdate));
            _parent.Add(Spacer());


            var scrubForwardButton = new Button
            {
                name = "ScrubForward",
                text = "Scrub +",
            };
            var scrubBackButton = new Button
            {
                name = "ScrubBack",
                text = "Scrub -",
            };
            _parent.Add(scrubBackButton);
            _parent.Add(scrubForwardButton);

            _parent.Add(Spacer());

            var randomizeMelody = new Button
            {
                name = "RandomizeMelody",
                text = "Randomize melody",
            };

            var randomizeRhythm = new Button
            {
                name = "RandomizeRhythm",
                text = "Randomize rhythm",
            };
            _parent.Add(randomizeMelody);
            _parent.Add(randomizeRhythm);

            _parent.Add(Spacer());
            _parent.Add(CreateUtilsBox());

            var utilsBox = new VisualElement()
            {
                style = { flexDirection = FlexDirection.Row }
            };
            var deleteButton = new Button
            {
                name = "DeleteButton",
                text = "Delete pattern",
                style =
                {
                    backgroundColor = new StyleColor(new Color(0.7f, 0.2f, 0.0f, 1))
                }
            };

            var clearButton = new Button
            {
                name = "ClearButton",
                text = "Clear pattern",
                style =
                {
                    backgroundColor = new StyleColor(new Color(0.7f, 0.2f, 0.0f, 1))
                }
            };
            utilsBox.Add(clearButton);
            utilsBox.Add(deleteButton);

            _parent.Add(Spacer());
            _parent.Add(utilsBox);
        }


        public static void DrawTrack(Action didUpdateInstrument)
        {
            _parent.Clear();
            Draw(_parent);
            var selection = AnysongEditorWindow.CurrentSelection.CurrentSongTrackProperty;
            var instrumentProperty = selection.FindPropertyRelative("instrument");


            _parent.Add(CreatePropertyFieldWithCallback(instrumentProperty, didUpdateInstrument));


            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("intensityMappingCurve"), null));


            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("voices"), null));


            var trackTypeIndexProperty = CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackTypeIndex"), null);
            trackTypeIndexProperty.label = "Track type";
            //_parent.Add(CreatePropertyFieldWithCallback(trackTypeProperty, null));
            _parent.Add(trackTypeIndexProperty);

            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("volume"), null));
            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("volumeMods"), null));

            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackPitch"), null));

            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("pitchMods"), null));

            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackAudioEnvelope1"),
                didUpdateInstrument));
            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackAudioEnvelope2"),
                didUpdateInstrument));

            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackAudioLFO1"), didUpdateInstrument));
            _parent.Add(CreatePropertyFieldWithCallback(selection.FindPropertyRelative("trackAudioLFO2"), didUpdateInstrument));


            _parent.Add(Spacer());

            foreach (var audioProcessorSettings in AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.TrackFilters)
            {
                VisualElement filterElement = new VisualElement
                {
                    style =
                    {
                        marginTop = 10,
                        paddingTop = 10,
                        borderTopWidth = 1,
                        borderTopColor = Color.gray
                    }
                };
                Button deleteFilter = new Button
                {
                    style =
                    {
                        position = Position.Absolute,
                        right = 0,
                        top = 5,
                        width = 20
                    },

                    text = "x"
                };

                deleteFilter.clicked += () => { RemoveFilter(audioProcessorSettings); };
                filterElement.Add(AudioProcessorInspector.Draw(audioProcessorSettings,
                    () => { AnysongEditorWindow.CurrentSong.RefreshSettings(); }));
                filterElement.Add(deleteFilter);
                _parent.Add(filterElement);
            }

            var addFilterButton = new Button
            {
                text = "Add effect",
                name = "AddFilterButton"
            };
            addFilterButton.clicked += () =>
            {
                var menu = new GenericMenu();
                foreach (AudioProcessorSettings.FilterTypes filterType in Enum.GetValues(
                             typeof(AudioProcessorSettings.FilterTypes)))
                {
                    menu.AddItem(new GUIContent(filterType.ToString()), false, () => { AddFilter(filterType); });
                }

                menu.ShowAsContext();
            };
            _parent.Add(addFilterButton);
        }

        private static void AddFilter(AudioProcessorSettings.FilterTypes filterType)
        {
            var newProcessor = new AudioProcessorSettings();
            newProcessor.Init();

            newProcessor.filterType = filterType;
            AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.AddAudioProcessor(newProcessor);
            EditorUtility.SetDirty(AnysongEditorWindow.CurrentSong);
            AssetDatabase.SaveAssets();
            AnysongEditorWindow.CurrentSong.RefreshEffects();
            // Refresh the inspector
            DrawTrack(null);
        }

        private static void RemoveFilter(AudioProcessorSettings filter)
        {
            AnysongEditorWindow.CurrentSelection.CurrentSongTrackSettings.RemoveAudioProcessor(filter);
            EditorUtility.SetDirty(AnysongEditorWindow.CurrentSong);
            AssetDatabase.SaveAssets();

            // Refresh the inspector
            DrawTrack(null);
        }

        public static void DrawProgression()
        {
            _parent.Clear();
            Draw(_parent);
            _parent.Add(Spacer());
            var progressionTypeHolder = new VisualElement();
            var progressionType =
                (AnysongSectionTrack.PatternProgressionType)AnysongEditorWindow.CurrentSelection.CurrentSectionTrackProperty
                    .FindPropertyRelative("patternProgressionType")
                    .enumValueIndex;


            _parent.Add(CreatePropertyFieldWithCallback(
                AnysongEditorWindow.CurrentSelection.CurrentSectionTrackProperty.FindPropertyRelative("patternProgressionType"),
                () =>
                {
                    progressionTypeHolder.Clear();
                    progressionTypeHolder.Add(DrawProgressionType((AnysongSectionTrack.PatternProgressionType)AnysongEditorWindow
                        .CurrentSelection
                        .CurrentSectionTrackProperty
                        .FindPropertyRelative("patternProgressionType")
                        .enumValueIndex, AnysongEditorWindow.CurrentSelection));
                    AnysongEditorWindow.CurrentSong.RefreshMidi(
                        AnysongEditorWindow.CurrentSelection.CurrentSectionIndex,
                        AnysongEditorWindow.CurrentSelection.CurrentTrackIndex, 
                        0);
                }));

            progressionTypeHolder.Add(DrawProgressionType(progressionType, AnysongEditorWindow.CurrentSelection));
            _parent.Add(progressionTypeHolder);
            _parent.Add(Spacer());
            _parent.Add(AnysongEditorWindow.CreateAddRemoveButtons(true, false));

            _parent.Add(Spacer());
        }

        private static VisualElement DrawProgressionType(AnysongSectionTrack.PatternProgressionType progressionType,
            AnysongEditorWindow.AnySelection selection)
        {
            var content = new VisualElement();
            switch (progressionType)
            {
                case AnysongSectionTrack.PatternProgressionType.Sequence:
                    var sequenceLabel = new Label("Patterns will play in sequence");
                    content.Add(sequenceLabel);
                    break;
                case AnysongSectionTrack.PatternProgressionType.WeightedRandom:
                    var weightedLabel = new Label("Patterns will be randomly selected by weight");
                    content.Add(weightedLabel);
                    content.Add(Spacer());
                    var triggerRowLabel = new Label()
                    {
                        style = { width = 100, }
                    };

                    var barRowLabel = new Label()
                    {
                        style = { width = 100, }
                    };


                    var patternHeaderRow = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Row }
                    };


                    barRowLabel.text = "";
                    patternHeaderRow.Add(barRowLabel);

                    triggerRowLabel.text = "Trigger chance";

                    var patternsHolder = new VisualElement();
                    for (var y = 0; y < selection.CurrentSectionTrack.patterns.Count; y++)
                    {
                        var pattern = selection.CurrentSectionTrack.patterns[y];
                        var patternRow = new VisualElement()
                        {
                            style = { flexDirection = FlexDirection.Row }
                        };
                        var patternLabel = new Label
                        {
                            text = "Pattern " + y,
                            style =
                            {
                                width = new StyleLength(new Length(15, LengthUnit.Percent))
                            }
                        };

                        patternRow.Add(patternLabel);


                        for (int i = 0; i < pattern.triggerChances.Count; i++)
                        {
                            var patternChance = new VisualElement()
                            {
                                style =
                                {
                                    flexDirection = FlexDirection.Column,
                                    width = new StyleLength(new Length(20, LengthUnit.Percent))
                                }
                            };

                            var property = selection.CurrentSectionTrackProperty.FindPropertyRelative("patterns")
                                .GetArrayElementAtIndex(y)
                                .FindPropertyRelative("triggerChances").GetArrayElementAtIndex(i);

                            int columnIndex = i; // Capture the column index for the callback
                            Button patternButton = new Button
                            {
                                style =
                                {
                                    width = new StyleLength(new Length(20, LengthUnit.Percent))
                                }
                            };

                            patternButton.BindProperty(property);

                            patternButton.clicked += () =>
                            {
                                pattern.triggerChances[columnIndex] =
                                    pattern.triggerChances[columnIndex] > 0 ? 0 : GetCollumnMaxValue(selection, columnIndex);
                                AdjustPatternWeights(selection, columnIndex);
                            };


                            var chanceSlider = new Slider
                            {
                                highValue = 100,
                                lowValue = 0,
                                value = pattern.triggerChances[i],
                                style =
                                {
                                    width = new StyleLength(new Length(20, LengthUnit.Percent))
                                }
                            };


                            chanceSlider.RegisterValueChangedCallback(evt => { AdjustPatternWeights(selection, columnIndex); });


                            chanceSlider.BindProperty(property);

                            patternRow.Add(patternButton);
                            //patternChance.Add(chanceSlider);
                            //patternRow.Add(patternChance);
                        }

                        patternsHolder.Add(patternRow);
                    }

                    content.Add(patternsHolder);
                    break;
                case AnysongSectionTrack.PatternProgressionType.Random:
                    var randomLabel = new Label("Patterns will be randomly selected every bar");
                    content.Add(randomLabel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return content;
        }

        static float GetCollumnMaxValue(AnysongEditorWindow.AnySelection selection, int columnIndex)
        {
            float totalWeight = 0f;
            for (var y = 0; y < selection.CurrentSectionTrack.patterns.Count; y++)
            {
                var pattern = selection.CurrentSectionTrack.patterns[y];
                float weight = pattern.triggerChances[columnIndex];
                if (weight > totalWeight)
                {
                    totalWeight = weight;
                }
            }

            if (totalWeight == 0f)
                return 100;
            return totalWeight;
        }

        static void AdjustPatternWeights(AnysongEditorWindow.AnySelection selection, int columnIndex)
        {
            List<float> columnWeights = new();
            float totalWeight = 0f;

            // Collect all current weights for this specific column and calculate total
            for (var y = 0; y < selection.CurrentSectionTrack.patterns.Count; y++)
            {
                var pattern = selection.CurrentSectionTrack.patterns[y];
                float weight = pattern.triggerChances[columnIndex];
                columnWeights.Add(weight);
                totalWeight += weight;
            }

            // Avoid division by zero
            if (totalWeight <= 0f)
            {
                for (var y = 0; y < selection.CurrentSectionTrack.patterns.Count; y++)
                {
                    var anysongPattern = selection.CurrentSectionTrack.patterns[y];
                    anysongPattern.triggerChances[columnIndex] = 0;
                }
            }
            else
            {
                // Normalize weights to sum to 100
                for (var y = 0; y < selection.CurrentSectionTrack.patterns.Count; y++)
                {
                    float normalizedWeight = (columnWeights[y] / totalWeight) * 100f;
                    var anysongPattern = selection.CurrentSectionTrack.patterns[y];
                    anysongPattern.triggerChances[columnIndex] = normalizedWeight;
                }
            }
        }


        public static void DrawNote(SerializedProperty note, Action didUpdate)
        {
            _parent.Clear();
            Draw(_parent);
            //var boxNotes = new Box();
            //boxNotes.Add(new Label("Note stuff"));
            //var notesBox = new Box()
            //{
            //    style = { flexDirection = FlexDirection.Row }
            //};

            //boxNotes.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("rootNote"), AnysongPatternView.Refresh));


            //var strumControl = new VisualElement();
            //strumControl.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("strumAmount"), didUpdate));
            //strumControl.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("strumRandom"), didUpdate));
            //strumControl.style.display =
            //    new StyleEnum<DisplayStyle>(step.FindPropertyRelative("chordNotes").arraySize > 0
            //        ? DisplayStyle.Flex
            //        : DisplayStyle.None);
//
            //boxNotes.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("chordNotes"), () =>
            //{
            //    strumControl.style.display =
            //        new StyleEnum<DisplayStyle>(step.FindPropertyRelative("chordNotes").arraySize > 0
            //            ? DisplayStyle.Flex
            //            : DisplayStyle.None);
            //    didUpdate?.Invoke();
            //}));


            //boxNotes.Add(strumControl);

            //boxNotes.Add(notesBox);


            //_parent.Add(boxNotes);

            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("noteIndex"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("drift"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("duration"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("velocity"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("chance"), didUpdate));


            var s = CreatePropertyFieldWithCallback(note.FindPropertyRelative("repeatRate"), didUpdate);

            _parent.Add(CreatePropertyFieldWithCallback(note.FindPropertyRelative("stepRepeats"),
                () =>
                {
                    didUpdate?.Invoke();
                    s.visible = note.FindPropertyRelative("stepRepeats").intValue > 0;
                }));


            _parent.Add(s);


            _parent.Add(Spacer());
            _parent.Add(CreateUtilsBox());
        }

        static VisualElement CreateUtilsBox()
        {
            var utilsBox = new Box()
            {
                style = { flexDirection = FlexDirection.Row }
            };
            var copyButton = new Button
            {
                name = "CopyButton",
                text = "Copy"
            };

            var pasteButton = new Button
            {
                name = "PasteButton",
                text = "Paste"
            };


            utilsBox.Add(copyButton);
            utilsBox.Add(pasteButton);
            return utilsBox;
        }


        static PropertyField CreatePropertyFieldWithCallback(SerializedProperty property, Action didUpdate)
        {
            var propertyField = new PropertyField(property);
            propertyField.BindProperty(property);
            propertyField.RegisterValueChangeCallback((ev) => { didUpdate?.Invoke(); });
            return propertyField;
        }
    }
}