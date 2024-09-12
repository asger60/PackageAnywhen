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
            
            
            //Label label = new Label(property.displayName)
            //{
            //    style =
            //    {
            //        minWidth = EditorGUIUtility.labelWidth // Match Unity's label width
            //    }
            //};
//
            //container.Add(label);
            Debug.Log(EditorGUI.indentLevel);
            ObjectField objectField = new ObjectField(property.displayName)
            {
                labelElement =
                {
                    style =
                    {
                        width = 200
                    }
                },
                objectType = typeof(AnywhenTrigger),
                allowSceneObjects = true,
                style =
                {
                    flexGrow = 1,
                }
            };
            //objectField.RegisterValueChangedCallback(evt =>
            //{
            //    property.serializedObject.ApplyModifiedProperties(); // Apply changes to the serialized object
            //});


            objectField.BindProperty(property);
            
            
            container.Add(objectField);

            if (property.objectReferenceValue == null)
            {
                var button = new Button
                {
                    text = "FIX",
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
                        Debug.Log("fix " + trigger);
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