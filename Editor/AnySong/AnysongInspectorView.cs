using System;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.Search.ObjectField;

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


        public static void DrawTrack(AnySongTrack track)
        {
            _parent.Clear();
            Draw(_parent);

            var fileField = new ObjectField("Instrument")
            {
                objectType = typeof(AnywhenInspector),
                value = track.instrument
            };

            var volumeSlider = new Slider(0, 1)
            {
                name = "Volume",
                value = track.volume,
                label = "Volume"
            };

            _parent.Add(fileField);
            _parent.Add(volumeSlider);
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
        }


        static PropertyField CreatePropertyFieldWithCallback(SerializedProperty property, Action didUpdate)
        {
            var propertyField = new PropertyField(property);
            propertyField.BindProperty(property);
            propertyField.RegisterValueChangeCallback((ev) => { didUpdate?.Invoke(); });
            //propertyField.RegisterCallback<ChangeEvent<bool>>((ev) =>
            //{
            //    if (ev.newValue != ev.previousValue)
            //        didUpdate?.Invoke();
            //});

            return propertyField;
        }
    }
}