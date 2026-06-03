using Unity.Collections;

namespace Anywhen.Synth
{
    public struct AudioProcessorBitcrush : IAudioProcessor, System.IDisposable
    {
        private float _bitDepth;
        private int _downsampling;
        private NativeArray<float> _lastSamples;
        private int _downsamplingCounter;
        private float _filterMod;
        AudioProcessorSettings.BitcrushSettings.Unmanaged _settings;

        public AudioProcessorBitcrush(int sampleRate)
        {
            _bitDepth = 0;
            _downsampling = 0;
            _downsamplingCounter = 0;
            _filterMod = 0;
            _lastSamples = new NativeArray<float>(2, Allocator.Persistent);
            _settings = new AudioProcessorSettings.BitcrushSettings().ToUnmanaged();
        }


        void UpdateSettings()
        {
            _bitDepth = _settings.bitDepth;
            _downsampling = _settings.downsampling;
            _filterMod = 1;
            //foreach (var mod in ModRoutings)
            //{
            //    _filterMod = mod.Process(_filterMod);
            //}
        }



        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings.bitcrushSettings;
            UpdateSettings();
        }


        public void DoUpdate()
        {
        }

        public void Process(NativeArray<float> buffer, AnysongTrack anysongTrack)
        {
            UpdateSettings();
            for (int frame = 0; frame < buffer.Length; frame++)
            {
                float sample = buffer[frame];
                // Bitcrush / Quantization
                if (_bitDepth < 24f)
                {
                    float levels = UnityEngine.Mathf.Pow(2, _bitDepth);
                    sample = UnityEngine.Mathf.Round(sample * levels) / levels;
                }

                // Downsampling
                if (_downsampling > 1)
                {
                    int channel = _downsamplingCounter % 2;
                    int step = _downsamplingCounter / 2;

                    int modifiedDownsampling = (int)(_downsampling * (_filterMod * 2));
                    if (step % modifiedDownsampling == 0)
                    {
                        _lastSamples[channel] = sample;
                    }

                    _downsamplingCounter++;
                    buffer[frame] = _lastSamples[channel];
                }

                //return sample;
            }
            

        }

        public void SetGate(bool gate)
        {
        }

        public void Dispose()
        {
            if (_lastSamples.IsCreated) _lastSamples.Dispose();
        }
    }
}