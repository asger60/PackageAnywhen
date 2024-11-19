using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


    [CustomEditor(typeof(AnywhenPlayerTrigger))]
    public class AnywhenPlayerTriggerInspector : UnityEditor.Editor
    {
        private VisualElement _targetObjectTypeElement;
        private VisualElement _targetObjectElement;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(AnywhenBranding.DrawBranding());
            _targetObjectTypeElement = new VisualElement();
            _targetObjectElement = new VisualElement();

            inspector.Add(new PropertyField(serializedObject.FindProperty("anywhenPlayer")));
            
            var triggerType = serializedObject.FindProperty("triggerType");

            var triggerTypeField = new PropertyField(triggerType);
            triggerTypeField.BindProperty(triggerType);
            inspector.Add(triggerTypeField);
            triggerTypeField.RegisterValueChangeCallback(evt =>
            {
                _targetObjectElement.Clear();
                _targetObjectTypeElement.Clear();
                _targetObjectTypeElement.Add(AnywhenTriggerInspector.AddTargetTypeControls(serializedObject, _targetObjectElement));
            });


            inspector.Add(_targetObjectTypeElement);
            inspector.Add(_targetObjectElement);
            
            inspector.Add(new PropertyField(serializedObject.FindProperty("triggerBehaviour")));
            inspector.Add(new PropertyField(serializedObject.FindProperty("transitionMode")));

            return inspector;
        }
        
    }
