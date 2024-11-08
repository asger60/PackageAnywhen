using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_2023_OR_NEWER
[UxmlElement]
#endif

public partial class AnySlider : Slider
{
    public new class UxmlFactory : UxmlFactory<AnySlider, UxmlTraits>
    {
    }


#if UNITY_2023_OR_NEWER
    [UxmlAttribute] public string unit { get; set; } = "%";
    [UxmlAttribute] public Color color { get; set; } = Color.cyan;
#else
    public string unit { get; set; } = "";
    public Color color { get; set; } = Color.yellow;

    public bool isEnabled { get; set; } = true;

#endif

#if !UNITY_2023_OR_NEWER
    public new class UxmlTraits : Slider.UxmlTraits
    {
        private readonly UxmlStringAttributeDescription _unit = new UxmlStringAttributeDescription { name = "unit", defaultValue = "" };
        private readonly UxmlColorAttributeDescription _color = new UxmlColorAttributeDescription { name = "color", defaultValue = Color.yellow };
        private readonly UxmlBoolAttributeDescription _isEnabled = new UxmlBoolAttributeDescription { name = "isEnabled", defaultValue = true };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var anySlider = (AnySlider)ve;
            anySlider.unit = _unit.GetValueFromBag(bag, cc);
            anySlider.color = _color.GetValueFromBag(bag, cc);
            anySlider.isEnabled = _isEnabled.GetValueFromBag(bag, cc);
        }
    }
#endif


    private readonly Label _valueLabel;
    private readonly VisualElement _dragTrack, _dragHandle;

    public AnySlider() : this((string)null)
    {
    }

    public AnySlider(float start, float end, SliderDirection direction = SliderDirection.Horizontal,
        float pageSize = 0.0f)
        : this((string)null, start, end, direction, pageSize)
    {
    }

    public AnySlider(
        string label,
        float start = 0.0f,
        float end = 10f,
        SliderDirection direction = SliderDirection.Horizontal,
        float pageSize = 0.0f)
        : base(label, start, end, direction, pageSize)
    {
        _valueLabel = new Label
        {
            name = "value-label"
        };
        _valueLabel.AddToClassList("value-label");

        var headerElement = new VisualElement
        {
            name = "header-element"
        };
        headerElement.AddToClassList("header-element");
        this.Add(headerElement);


        var dragContainerElement = new VisualElement
        {
            name = "drag-element"
        };
        dragContainerElement.AddToClassList("drag-element");
        this.Add(dragContainerElement);

        headerElement.Add(labelElement);
        headerElement.Add(_valueLabel);

        AddToClassList("anywhen-slider");
        var dragContainerLine = new VisualElement();
        dragContainerLine.name = "drag-container-line";
        dragContainerLine.AddToClassList("drag-container-line");
        dragContainerElement.Add(dragContainerLine);

        dragContainerElement.Add(this.Q<VisualElement>("unity-drag-container").parent);

        labelElement.AddToClassList("slider-label");
        var dragContainer = this.Q<VisualElement>("unity-drag-container");
        dragContainer?.AddToClassList("unity-drag-container");

        _dragHandle = this.Q<VisualElement>("unity-dragger");
        _dragHandle?.AddToClassList("drag-handle");
        _dragHandle.style.backgroundColor = color;

        _dragTrack = this.Q<VisualElement>("unity-tracker");
        _dragTrack?.AddToClassList("drag-track");
        _dragTrack.style.backgroundColor = new StyleColor(color);

        var dragBorder = this.Q<VisualElement>("unity-dragger-border");
        dragBorder?.AddToClassList("drag-handle-border");


       
    }

    public void SetIsEnabled(bool state)
    {
        isEnabled = state;

        _dragHandle.style.backgroundColor = !isEnabled ? new StyleColor(Color.gray) : new StyleColor(color);
        _dragTrack.style.backgroundColor = !isEnabled ? new StyleColor(Color.gray) : new StyleColor(color);
    }

    public override void SetValueWithoutNotify(float newValue)
    {
        if (!isEnabled) return;
        base.SetValueWithoutNotify(newValue);
        _valueLabel.text = newValue + " " + unit;
        float lengthPercent = Mathf.InverseLerp(lowValue, highValue, newValue) * 100;
        _dragTrack.style.width = new StyleLength(new Length(lengthPercent, LengthUnit.Percent));
        _dragTrack.style.backgroundColor = new StyleColor(color);
        _dragHandle.style.backgroundColor = color;
    }
}