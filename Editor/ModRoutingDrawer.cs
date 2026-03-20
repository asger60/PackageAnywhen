using Anywhen.Synth.Filter;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SynthFilterBase.ModRouting))]
public class ModRoutingDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var modSourceProp = property.FindPropertyRelative("modSource");
        var modAmountProp = property.FindPropertyRelative("modAmount");
        var modDepthProp  = property.FindPropertyRelative("modDepth");

        var root = new VisualElement();

        // Mod Source
        var sourceField = new PropertyField(modSourceProp, "Mod Source");

        // Mod Amount (shown when Envelope)
        var amountField = new PropertyField(modAmountProp, "Mod Amount");

        // Mod Depth (shown when LFO)
        var depthField = new PropertyField(modDepthProp, "Mod Depth");

        root.Add(sourceField);
        root.Add(amountField);
        root.Add(depthField);

        // Set initial visibility
        Refresh(modSourceProp.enumValueIndex);

        // Update visibility when source changes
        sourceField.RegisterValueChangeCallback(evt =>
            Refresh(modSourceProp.enumValueIndex));

        return root;

        void Refresh(int sourceIndex)
        {
            bool isLFO = sourceIndex == (int)SynthFilterBase.ModRouting.ModSources.LFO;
            amountField.style.display = isLFO ? DisplayStyle.None : DisplayStyle.Flex;
            depthField.style.display  = isLFO ? DisplayStyle.Flex  : DisplayStyle.None;
        }
    }
}