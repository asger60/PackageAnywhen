using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongInspectorView
    {
        private static VisualElement _parent;

        public static void Draw(VisualElement parent)
        {
            _parent = parent;
            _parent.Add(new Label("Inspector"));
        }

        static VisualElement Spacer()
        {
            var spacer = new VisualElement()
            {
                style = { minHeight = 10 }
            };
            return spacer;
        }

        public static void DrawPattern(SerializedProperty pattern, bool isBase, Action didUpdate)
        {
            _parent.Clear();
            Draw(_parent);


            _parent.Add(Spacer());

            //var triggerRowLabel = new Label()
            //{
            //    style = { width = 100, }
            //};
//
            //var barRowLabel = new Label()
            //{
            //    style = { width = 100, }
            //};
//
//
            //var patternsBox = new Box();
//
            //var patternHeaderRow = new VisualElement()
            //{
            //    style = { flexDirection = FlexDirection.Row }
            //};
//
            //var patternRow = new VisualElement()
            //{
            //    style = { flexDirection = FlexDirection.Row }
            //};
//
            //barRowLabel.text = "";
            //patternHeaderRow.Add(barRowLabel);
//
            //triggerRowLabel.text = "Trigger chance";
            //patternRow.Add(triggerRowLabel);
//
            //var triggerArrayProperty = pattern.FindPropertyRelative("triggerChances");
//
            //for (var i = 0; i < triggerArrayProperty.arraySize; i++)
            //{
            //    var property = triggerArrayProperty.GetArrayElementAtIndex(i);
//
            //    var chanceField = new FloatField
            //    {
            //        value = triggerArrayProperty.GetArrayElementAtIndex(i).floatValue,
            //        style = { minWidth = 30, },
            //        focusable = !isBase,
            //    };
            //    chanceField.BindProperty(property);
//
            //    var barLabel = new Label()
            //    {
            //        text = i.ToString(),
            //        style =
            //        {
            //            justifyContent = new StyleEnum<Justify>(Justify.Center),
            //            alignContent = new StyleEnum<Align>(Align.Center),
            //            minWidth = 37,
            //            paddingLeft = 11,
            //        },
            //    };
//
            //    patternHeaderRow.Add(barLabel);
            //    patternRow.Add(chanceField);
            //}
//
            //patternsBox.Add(new Label("Triggering"));
            //patternsBox.Add(Spacer());
            //patternsBox.Add(patternHeaderRow);
            //patternsBox.Add(patternRow);
            //_parent.Add(patternsBox);
//
//
            //_parent.Add(Spacer());

            _parent.Add(CreatePropertyFieldWithCallback(pattern.FindPropertyRelative("rootNote"), null));
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
            utilsBox.Add(deleteButton);
            _parent.Add(Spacer());
            _parent.Add(utilsBox);
        }


        public static void DrawTrack(AnysongEditorWindow.AnySelection selection)
        {
            _parent.Clear();
            Draw(_parent);

            _parent.Add(
                CreatePropertyFieldWithCallback(selection.CurrentSongTrackProperty.FindPropertyRelative("instrument"),
                    null));
            _parent.Add(
                CreatePropertyFieldWithCallback(selection.CurrentSongTrackProperty.FindPropertyRelative("volume"),
                    null));
            _parent.Add(CreatePropertyFieldWithCallback(
                selection.CurrentSectionTrackProperty.FindPropertyRelative("intensityMappingCurve"), null));
        }

        public static void DrawProgression(
            AnysongEditorWindow.AnySelection selection /*AnysongSectionTrack track, SerializedProperty trackProperty*/)
        {
            _parent.Clear();
            Draw(_parent);
            _parent.Add(Spacer());

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
            //var barLabel = new Label()
            //{
            //    text = i.ToString(),
            //    style =
            //    {
            //        justifyContent = new StyleEnum<Justify>(Justify.Center),
            //        alignContent = new StyleEnum<Align>(Align.Center),
            //        minWidth = 37,
            //        paddingLeft = 11,
            //    },
            //};


            //List<SerializedProperty> propertyRows = new List<SerializedProperty>();
            //for (var y = 0; y < track.patterns.Count; y++)
            //{
            //    var property = trackProperty.FindPropertyRelative("patterns").GetArrayElementAtIndex(y)
            //        .FindPropertyRelative("triggerChances").GetArrayElementAtIndex(0);
            //    propertyRows.Add(property);
            //}

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
                        minWidth = 100,
                        width = 100
                    }
                };

                patternRow.Add(patternLabel);


                for (int i = 0; i < pattern.triggerChances.Count; i++)
                {
                    var chanceField = new FloatField
                    {
                        value = pattern.triggerChances[i],
                        style = { minWidth = 30, },
                    };


                    var property = selection.CurrentSectionTrackProperty.FindPropertyRelative("patterns")
                        .GetArrayElementAtIndex(y)
                        .FindPropertyRelative("triggerChances").GetArrayElementAtIndex(i);
                    chanceField.BindProperty(property);

                    //chanceField.RegisterValueChangedCallback((evt) =>
                    //{
                    //    Debug.Log("value changed " + evt.newValue);
                    //    EvaluateOtherTriggerChanced(propertyRows);
                    //});

                    patternRow.Add(chanceField);
                }

                patternsHolder.Add(patternRow);
            }

            _parent.Add(patternsHolder);

            _parent.Add(AnysongEditorWindow.CreateAddRemoveButtons(true, false));

            _parent.Add(Spacer());
        }


        static void EvaluateOtherTriggerChanced(List<SerializedProperty> properties)
        {
            foreach (var property in properties)
            {
                Debug.Log(property.floatValue);
                //property.floatValue = 0;
            }
        }

        public static void DrawStep(SerializedProperty step, Action didUpdate)
        {
            _parent.Clear();
            Draw(_parent);
            var boxTriggering = new Box();
            boxTriggering.Add(new Label("Triggering"));
            boxTriggering.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("noteOn"), didUpdate));
            boxTriggering.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("noteOff"), didUpdate));
            _parent.Add(boxTriggering);

            _parent.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("duration"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("offset"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("velocity"), didUpdate));
            _parent.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("chance"), didUpdate));

            var boxNotes = new Box();
            boxNotes.Add(new Label("Note stuff"));
            var notesBox = new Box()
            {
                style = { flexDirection = FlexDirection.Row }
            };

            boxNotes.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("rootNote"), didUpdate));
            boxNotes.Add(CreatePropertyFieldWithCallback(step.FindPropertyRelative("chordNotes"), didUpdate));

            boxNotes.Add(notesBox);


            _parent.Add(boxNotes);
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