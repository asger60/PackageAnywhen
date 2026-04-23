
namespace Anywhen.Synth
{
    public interface IAudioProcessor
    {
        public void DoUpdate();

        public float Process(float sample);

        public void SetGate(bool gate);
    }

    public struct TrackAudioProcessor
    {
        private SynthSettingsObjectFilter.Unmanaged _settings;
        private AudioProcessorLowPass _lowPass;
        // Add other filter states here as they are ported to structs

        public TrackAudioProcessor(int sampleRate, SynthSettingsObjectFilter.Unmanaged settings)
        {
            _settings = settings;
            _lowPass = new AudioProcessorLowPass(sampleRate);
            
            // Initialize defaults to avoid issues if not LowPass type
            _lowPass.SetOversampling(1);
            _lowPass.SetCutOff(24000);
            _lowPass.SetResonance(0);

            // Initialize based on type
            if (_settings.filterType == SynthSettingsObjectFilter.FilterTypes.LowPassFilter)
            {
                _lowPass.SetCutOff(_settings.lowPassSettings.cutoffFrequency);
                _lowPass.SetResonance(_settings.lowPassSettings.resonance);
                _lowPass.SetOversampling(_settings.lowPassSettings.oversampling);
            }
        }

        public float Process(float sample)
        {
            switch (_settings.filterType)
            {
                case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:
                    return _lowPass.Process(sample);
                default:
                    return sample;
            }
        }
    }
}