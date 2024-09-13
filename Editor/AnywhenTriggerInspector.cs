using System;
using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(AnywhenTrigger))]
    public class AnywhenTriggerInspector : UnityEditor.Editor
    {
        private VisualElement _targetObjectTypeElement;
        private VisualElement _targetObjectElement;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            _targetObjectTypeElement = new VisualElement();
            _targetObjectElement = new VisualElement();


            var triggerType = serializedObject.FindProperty("triggerType");

            var triggerTypeField = new PropertyField(triggerType);
            triggerTypeField.BindProperty(triggerType);
            inspector.Add(triggerTypeField);
            triggerTypeField.RegisterValueChangeCallback(evt =>
            {
                _targetObjectElement.Clear();
                _targetObjectTypeElement.Clear();
                _targetObjectTypeElement.Add(AddTargetTypeControls());
            });


            inspector.Add(_targetObjectTypeElement);
            inspector.Add(_targetObjectElement);
            return inspector;
        }

        VisualElement AddTargetTypeControls()
        {
            VisualElement inspector = new VisualElement();
            var triggerType = serializedObject.FindProperty("triggerType");

            if ((AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.CollisionEnter ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.CollisionExit ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.CollisionEnter2D ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.CollisionExit2D ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.TriggerEnter ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.TriggerExit ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.TriggerEnter2D ||
                (AnywhenTrigger.TriggerTypes)triggerType.enumValueIndex == AnywhenTrigger.TriggerTypes.TriggerExit2D)
            {
                var triggerObjectType = serializedObject.FindProperty("triggerObjectType");
                var triggerObjectTypeField = new PropertyField(triggerObjectType);
                triggerObjectTypeField.BindProperty(triggerObjectType);

                inspector.Add(triggerObjectTypeField);
                triggerObjectTypeField.RegisterValueChangeCallback(evt =>
                {
                    _targetObjectElement.Clear();
                    _targetObjectElement.Add(AddTargetObjectControls());
                });
            }

            return inspector;
        }

        VisualElement AddTargetObjectControls()
        {
            VisualElement inspector = new VisualElement();
            var triggerType = serializedObject.FindProperty("triggerObjectType");

            switch ((AnywhenTrigger.TriggerObjectTypes)triggerType.enumValueIndex)
            {
                case AnywhenTrigger.TriggerObjectTypes.GameObject:
                {
                    var targetObject = serializedObject.FindProperty("triggerObject");
                    var targetObjectField = new PropertyField(targetObject);
                    targetObjectField.BindProperty(targetObject);
                    inspector.Add(targetObjectField);
                    break;
                }
                case AnywhenTrigger.TriggerObjectTypes.Tag:
                    var targetTag = serializedObject.FindProperty("triggerObjectTag");
                    var targetTagField = new TagField("Trigger tag");
                    targetTagField.labelElement.AddToClassList(PropertyField.inspectorElementUssClassName);

                    //targetTagField.labelElement.style.width = new StyleLength(new Length(40, LengthUnit.Percent));

                    targetTagField.BindProperty(targetTag);
                    inspector.Add(targetTagField);
                    break;
                case AnywhenTrigger.TriggerObjectTypes.Name:
                    var targetName = serializedObject.FindProperty("triggerObjectName");
                    var targetNameField = new PropertyField(targetName);
                    targetNameField.BindProperty(targetName);
                    inspector.Add(targetNameField);
                    break;
                case AnywhenTrigger.TriggerObjectTypes.Any:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return inspector;
        }
    }
}