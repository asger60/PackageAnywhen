namespace Anywhen.Synth.Filter
{
    public abstract class SynthFilterBase
    {
        public SynthSettingsObjectFilter Settings { get; protected set; }
        public abstract void SetExpression(float data);

        public abstract void SetSettings(SynthSettingsObjectFilter newSettings);
        
        public abstract void SetParameters(SynthSettingsObjectFilter settingsObjectFilter);

        public abstract void HandleModifiers(float mod1);
        

        public virtual float Process(float sample)
        {
            return sample;
        }
    }
}