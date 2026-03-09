namespace Anywhen.Synth.Filter
{
    public abstract class SynthFilterBase
    {
        protected SynthSettingsObjectFilter Settings;
        public abstract void SetExpression(float data);

        public abstract void SetSettings(SynthSettingsObjectFilter newSettings);
        
        public abstract void SetParameters(SynthSettingsObjectFilter settingsObjectFilter);

        public abstract void HandleModifiers(float mod1);
        
        public virtual void process_mono_stride(float[] samples, int sampleCount, int offset, int stride)
        {
        }

        public virtual float Process(float sample)
        {
            return sample;
        }
    }
}