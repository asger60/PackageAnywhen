namespace Anywhen.Synth
{
    public struct AudioProcessorLFO : IAudioProcessor
    {
        // FIX 1: uint so overflow wraps naturally at 2^32,
        //         matching the PhaseMax constant design intent
        private uint _phase;

        private const float PhaseMax = 4294967296f; // 2^32
        private uint _freqPhPSmp;
        private bool _isActive;

        private float _currentFrequency;
        bool _retrigger;
        AudioProcessorSettings.LFOSettings _settings;
        private int _sampleRate;

        public AudioProcessorLFO(int sampleRate)
        {
            _sampleRate = sampleRate;
            _currentFrequency = 0.5f;
            _retrigger = false;
            _freqPhPSmp = 0u;
            _phase = 0u;
            _isActive = false;
            _settings = default;
            UpdateSettings();
        }

        void UpdateSettings()
        {
            _currentFrequency = _settings.frequency;
            SetFreq(_settings.frequency);
            _isActive = true;
        }

        private void Restart()
        {
            if (!_isActive) return;
            _phase = 0u;
            _isActive = true;
            SetFreq(_currentFrequency);
        }

        public void DoUpdate() { }

        public float Process(float current, AnysongTrack anysongTrack)
        {
            // FIX 2: removed "_phase = current" — that was clobbering the
            //         phase accumulator with the audio input every single sample.
            //         The phase must only ever be incremented.
            _phase += _freqPhPSmp;

            float sin = Sin();

            if (_settings.unipolar)
                return (sin + 1f) * 0.5f;

            return sin;
        }

        public void SetGate(bool gate) { }
        
        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            
            
        }


        public void SetSettings(AudioProcessorSettings.LFOSettings settings)
        {
            _settings = settings;
            UpdateSettings();
        }

        public void NoteOn()
        {
            if (_retrigger)
                Restart();
        }

        private void SetFreq(float freqHz)
        {
            float freqPpsmp = freqHz / _sampleRate;

            if (float.IsNaN(freqPpsmp) || float.IsInfinity(freqPpsmp))
                freqPpsmp = 0;

            _freqPhPSmp = (uint)(freqPpsmp * PhaseMax);
        }

        private float Sin()
        {
            // Use a faster parabolic sine approximation
            // ph maps [0 .. 1)
            float x = (_phase / PhaseMax) * 2f - 1f; // map to [-1, 1]
        
            // y = 4x(1 - |x|)
            float y = 4f * x * (1f - (x < 0 ? -x : x));
        
            // Extra precision: y = 0.225(y(|y| - 1) + y)
            // For LFO, the basic version is often enough, but let's keep it smooth.
            // float absY = y < 0 ? -y : y;
            // y = 0.225f * (y * (absY - 1f)) + y;

            return y;
        }
    }
}