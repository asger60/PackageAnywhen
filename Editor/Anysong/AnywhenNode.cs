

using Unity.VisualScripting;

public class AnywhenNode : Unit
{

    
    [DoNotSerialize] // No need to serialize ports.
    public ControlInput inputTrigger; //Adding the ControlInput port variable

    [DoNotSerialize] // No need to serialize ports.
    public ControlOutput outputTrigger;//Adding the ControlOutput port variable.

    protected override void Definition()
    {
        //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
        inputTrigger = ControlInput("inputTrigger", (flow) => { return outputTrigger; });
        //Making the ControlOutput port visible and setting its key.
        outputTrigger = ControlOutput("outputTrigger");
    }
}