using Anywhen.Composing;
using Unity.VisualScripting;
using UnityEngine.UI;


[UnitTitle("Anywhen control")]
public class AnywhenControl : Unit
{
    [DoNotSerialize] // No need to serialize ports
    public ValueInput intensity; // Adding the ValueInput variable for myValueA

    [PortLabelHidden][DoNotSerialize] // No need to serialize ports
    public ValueOutput result; // Adding the ValueOutput variable for result

    [PortLabelHidden][DoNotSerialize] // No need to serialize ports.
    public ControlInput input; //Adding the ControlInput port variable


    //private string resultValue; // Adding the string variable for the processed result value

    protected override void Definition()
    {
        input = ControlInput("input", (flow) =>
        {
            AnysongPlayerBrain.SetGlobalIntensity(flow.GetValue<float>(intensity));
            return null;
        });
        
        intensity = ValueInput<float>("Intensity");
    }
    
    
}