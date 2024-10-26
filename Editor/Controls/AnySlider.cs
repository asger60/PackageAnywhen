using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AnySlider : Slider
{
    [UxmlAttribute] public string myString { get; set; } = "default_value";

    [UxmlAttribute] public float myFloat { get; set; } = 2;
    [UxmlAttribute] public float color { get; set; } = 2;


    public static readonly new string ussClassName = "anywhen-slider";
    public static readonly new string labelUssClassName = "slider-label";
    public new static readonly string draggerUssClassName = "drag-handle";
    public new static readonly string draggerBorderUssClassName = "drag-handle-border";
    public new static readonly string trackerUssClassName = "drag-track";

    private readonly Label _valueLabel;
    private readonly VisualElement _dragTrack;


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

        AddToClassList(ussClassName);
        var dragContainerLine = new VisualElement();
        dragContainerLine.name = "drag-container-line";
        dragContainerLine.AddToClassList("drag-container-line");
        dragContainerElement.Add(dragContainerLine);

        dragContainerElement.Add(this.Q<VisualElement>("unity-drag-container").parent);

        labelElement.AddToClassList(labelUssClassName);
        var dragContainer = this.Q<VisualElement>("unity-drag-container");
        dragContainer?.AddToClassList("unity-drag-container");

        var dragHandle = this.Q<VisualElement>("unity-dragger");
        dragHandle?.AddToClassList(draggerUssClassName);

        _dragTrack = this.Q<VisualElement>("unity-tracker");
        _dragTrack?.AddToClassList(trackerUssClassName);

        var dragBorder = this.Q<VisualElement>("unity-dragger-border");
        dragBorder?.AddToClassList(draggerBorderUssClassName);
        

    }

    public override void SetValueWithoutNotify(float newValue)
    {
        base.SetValueWithoutNotify(newValue);
        _valueLabel.text = newValue.ToString();
        float lengthPercent = Mathf.InverseLerp(lowValue, highValue, newValue) * 100;
        _dragTrack.style.width = new StyleLength(new Length(lengthPercent, LengthUnit.Percent));
    }
}