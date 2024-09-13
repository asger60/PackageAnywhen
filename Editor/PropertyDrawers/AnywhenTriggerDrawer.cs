using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(AnywhenTrigger))]
    public class AnywhenTriggerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };

            VisualElement container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };

            

            PropertyField propertyField = new PropertyField(property)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            container.Add(propertyField);
            propertyField.BindProperty(property);

            if (property.objectReferenceValue == null)
            {
                var button = new Button
                {
                    text = "Locate",
                    style =
                    {
                        width = 100,
                    }
                };
                button.clicked += () =>
                {
                    MonoBehaviour target = (MonoBehaviour)property.serializedObject.targetObject;
                    var trigger = target.GetComponent<AnywhenTrigger>();
                    if (trigger)
                    {
                        property.objectReferenceValue = trigger;
                        property.serializedObject.ApplyModifiedProperties();
                        CreatePropertyGUI(property);
                        container.Remove(button);
                    }
                };
                container.Add(button);
            }


            root.Add(container);
            return root;
        }
    }
}