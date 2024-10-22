using System;
using Anywhen;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(AnywhenIntensitySetter))]
    public class AnywhenIntensitySetterInspector : UnityEditor.Editor
    {
        private VisualElement _targetObjectTypeElement;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(AnywhenBranding.DrawBranding());
            
            _targetObjectTypeElement = new VisualElement();
            var trigger = serializedObject.FindProperty("anywhenTrigger");
            inspector.Add(new PropertyField(trigger));

            var triggerType = serializedObject.FindProperty("intensityUpdateMode");

            var triggerTypeField = new PropertyField(triggerType);
            triggerTypeField.BindProperty(triggerType);
            inspector.Add(triggerTypeField);
            triggerTypeField.RegisterValueChangeCallback(evt =>
            {
                _targetObjectTypeElement.Clear();
                _targetObjectTypeElement.Add(AddTargetTypeControls());
            });


            inspector.Add(_targetObjectTypeElement);
            return inspector;
        }

        VisualElement AddTargetTypeControls()
        {
            VisualElement inspector = new VisualElement();
            var triggerType = serializedObject.FindProperty("intensityUpdateMode");

            if ((AnywhenIntensitySetter.ValueChangeModes)triggerType.enumValueIndex == AnywhenIntensitySetter.ValueChangeModes.Modify)
            {
                var label = new Label("The music intensity will be modified by this amount everytime this component is triggered.");
                label.style.height = 32;
                label.style.paddingTop = new StyleLength(8);
                label.style.paddingLeft = new StyleLength(4);
                
                inspector.Add(label);
                var triggerObjectType = serializedObject.FindProperty("intensityModifyValue");
                var triggerObjectTypeField = new PropertyField(triggerObjectType);
                triggerObjectTypeField.BindProperty(triggerObjectType);
                inspector.Add(triggerObjectTypeField);
            }
            
            if ((AnywhenIntensitySetter.ValueChangeModes)triggerType.enumValueIndex == AnywhenIntensitySetter.ValueChangeModes.Set)
            {
                var label = new Label("The music intensity will be set to this exact value everytime this component is triggered");
                label.style.height = 32;
                label.style.textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis);
                label.style.paddingTop = new StyleLength(8);
                label.style.paddingLeft = new StyleLength(4);
                inspector.Add(label);
                var triggerObjectType = serializedObject.FindProperty("intensitySetValue");
                var triggerObjectTypeField = new PropertyField(triggerObjectType);
                triggerObjectTypeField.BindProperty(triggerObjectType);
                inspector.Add(triggerObjectTypeField);
            }

            return inspector;
        }


    }
}