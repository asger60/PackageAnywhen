namespace Anywhen.Synth
{
    public struct AudioSourceSynth : IAudioSource
    {
        private int _sampleRate;
        AudioSourceSettings.SynthSourceSettings.Unmanaged _settings;


        public AudioSourceSynth(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
        }

        public void DoUpdate()
        {
        }

        public void SetSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings.synthSourceSettings.ToUnmanaged();
        }

        public float Process(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            return 0;
        }

        public void SetGate(bool gate)
        {
        }


        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}