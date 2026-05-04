namespace Anywhen.Synth
{
    public struct AudioSourceSample : IAudioSource
    {
        private int _sampleRate;
        AudioSourceSettings.SampleSourceSettings.Unmanaged _settings;


        public AudioSourceSample(int sampleRate) : this()
        {
            _sampleRate = sampleRate;
        }

        public void DoUpdate()
        {
        }

        public void SetSettings(AudioSourceSettings.Unmanaged settings)
        {
            _settings = settings.sampleSourceSettings.ToUnmanaged();
        }

        public float Process(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

            return 0;
        }




        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}