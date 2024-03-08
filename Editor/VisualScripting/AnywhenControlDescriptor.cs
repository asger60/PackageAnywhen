using Unity.VisualScripting;

[Descriptor(typeof(AnywhenControl))]
public class AnywhenControlDescriptor : UnitDescriptor<AnywhenControl>
{
    public AnywhenControlDescriptor(AnywhenControl unit) : base(unit) {}

    protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
    {
        base.DefinedPort(port, description);
        switch (port.key)
        {
            case "input":
                description.summary = "Trigger the sending of the intensity input.";
                break;
            case "Intensity":
                description.summary = "Set the global intensity in the AnysongPlayerBrain";
                break;
        }
    }
}