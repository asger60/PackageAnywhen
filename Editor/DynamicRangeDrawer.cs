using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(DynamicRangeAttribute))]
public class DynamicRangeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var valueProp = property.FindPropertyRelative("value");
        var maxProp = property.FindPropertyRelative("max");

        // Root — mimic Unity's own BaseField row layout
        var root = new VisualElement();
        root.AddToClassList("unity-base-field");

        // Label — uses Unity's own label class so it aligns with all other fields
        var label = new Label(property.displayName);
        label.AddToClassList("unity-base-field__label");

        // Input area — fills the right side like Unity's own controls
        var input = new VisualElement();
        input.AddToClassList("unity-base-field__input");
        input.style.flexDirection = FlexDirection.Row;
        input.style.alignItems = Align.Center;
        input.style.overflow = Overflow.Hidden;


        // Slider
        var slider = new Slider(0, maxProp.floatValue)
        {
            value = valueProp.floatValue,
            style =
            {
                flexGrow = 1,
            }
        };


        // Value field
        var valueField = MakeCompactFloatField(valueProp.floatValue, 40);

        // Max field
        var maxField = MakeCompactFloatField(maxProp.floatValue, 30);

        input.Add(slider);
        input.Add(valueField);
        input.Add(maxField);

        root.Add(label);
        root.Add(input);

        // --- Helpers ---
        void RefreshSliderRange()
        {
            float lo = 0;
            float hi = maxProp.floatValue;
            bool valid = lo < hi;
            slider.SetEnabled(valid);
            if (valid)
            {
                slider.lowValue = lo;
                slider.highValue = hi;
            }
        }

        void ApplyValue(float raw)
        {
            float v = Mathf.Clamp(raw, 0, maxProp.floatValue);
            valueProp.floatValue = v;
            slider.SetValueWithoutNotify(v);
            valueField.SetValueWithoutNotify(v);
            property.serializedObject.ApplyModifiedProperties();
        }


        void ApplyMax(float raw)
        {
            float v = Mathf.Max(raw, 0);
            maxProp.floatValue = v;
            maxField.SetValueWithoutNotify(v);
            property.serializedObject.ApplyModifiedProperties();
            RefreshSliderRange();
            ApplyValue(valueProp.floatValue);
        }

        slider.RegisterValueChangedCallback(evt => ApplyValue(evt.newValue));
        valueField.RegisterValueChangedCallback(evt => ApplyValue(evt.newValue));
        maxField.RegisterValueChangedCallback(evt => ApplyMax(evt.newValue));

        RefreshSliderRange();

        return root;
    }

    // FloatField with its internal label stripped out so it takes no extra space
    private static FloatField MakeCompactFloatField(float initialValue, float width)
    {
        var field = new FloatField
        {
            value = initialValue,
            style =
            {
                width = width
            }
        };

        var inner = field.Q<Label>();
        if (inner != null)
        {
            inner.style.display = DisplayStyle.None;
        }

        return field;
    }
}