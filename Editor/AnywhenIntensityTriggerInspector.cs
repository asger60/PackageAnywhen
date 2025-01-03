using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


    [CustomEditor(typeof(AnywhenIntensityTrigger))]
    public class AnywhenIntensityTriggerInspector : Editor
    {
        private VisualElement _targetObjectTypeElement;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(AnywhenBranding.DrawBranding());


            var triggerType = serializedObject.FindProperty("triggerType");

            var triggerTypeField = new PropertyField(triggerType);
            triggerTypeField.BindProperty(triggerType);
            inspector.Add(triggerTypeField);
            
            var triggerTypeElement = new VisualElement();
            var triggerObjectTypeElement = new VisualElement();
            
            triggerTypeField.RegisterValueChangeCallback(evt =>
            {
                triggerTypeElement.Clear();
                triggerObjectTypeElement.Clear();
                triggerObjectTypeElement.Add( AnywhenTriggerInspector.AddTargetTypeControls(serializedObject, triggerTypeElement));
            });
            
            inspector.Add(triggerObjectTypeElement);
            inspector.Add(triggerTypeElement);
            
            _targetObjectTypeElement = new VisualElement();
            var trigger = serializedObject.FindProperty("trigger");
            inspector.Add(new PropertyField(trigger));

            var intensityTarget = serializedObject.FindProperty("intensityTarget");
            var intensityTargetProperty = new PropertyField(intensityTarget);


            inspector.Add(intensityTargetProperty);


            var targetElement = new VisualElement
            {
                style =
                {
                    display = new StyleEnum<DisplayStyle>(intensityTarget.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None)
                }
            };


            var targetPlayer = serializedObject.FindProperty("targetPlayer");
            targetElement.Add(new PropertyField(targetPlayer));

            intensityTargetProperty.RegisterValueChangeCallback(evt =>
            {
                targetElement.style.display =
                    new StyleEnum<DisplayStyle>(evt.changedProperty.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None);
            });
            
            inspector.Add(targetElement);

            var intensityUpdateMode = serializedObject.FindProperty("intensityUpdateMode");
            var intensityUpdateModeField = new PropertyField(intensityUpdateMode);
            intensityUpdateModeField.BindProperty(intensityUpdateMode);
            inspector.Add(intensityUpdateModeField);
            intensityUpdateModeField.RegisterValueChangeCallback(evt =>
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

            if ((AnywhenIntensityTrigger.ValueChangeModes)triggerType.enumValueIndex == AnywhenIntensityTrigger.ValueChangeModes.Modify)
            {
                var label = new Label("The music intensity will be modified by this amount everytime this component is triggered.")
                {
                    style =
                    {
                        height = 32,
                        paddingTop = new StyleLength(8),
                        paddingLeft = new StyleLength(4)
                    }
                };

                inspector.Add(label);
                var triggerObjectType = serializedObject.FindProperty("intensityModifyValue");
                var triggerObjectTypeField = new PropertyField(triggerObjectType);
                triggerObjectTypeField.BindProperty(triggerObjectType);
                inspector.Add(triggerObjectTypeField);
            }

            if ((AnywhenIntensityTrigger.ValueChangeModes)triggerType.enumValueIndex == AnywhenIntensityTrigger.ValueChangeModes.Set)
            {
                var label = new Label("The music intensity will be set to this exact value everytime this component is triggered")
                {
                    style =
                    {
                        height = 32,
                        textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis),
                        paddingTop = new StyleLength(8),
                        paddingLeft = new StyleLength(4)
                    }
                };
                inspector.Add(label);
                var triggerObjectType = serializedObject.FindProperty("intensitySetValue");
                var triggerObjectTypeField = new PropertyField(triggerObjectType);
                triggerObjectTypeField.BindProperty(triggerObjectType);
                inspector.Add(triggerObjectTypeField);
            }

            return inspector;
        }
    }
