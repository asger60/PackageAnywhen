using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    public struct AudioProcessorReverb : IAudioProcessor, System.IDisposable
    {
        // Classic Schroeder reverb:
        // 4 parallel comb filters → summed → 2 series allpass filters

        // Prime-ish delay lengths (in samples at 44100 Hz) spread across the stereo field.
        // Slightly different per channel to avoid a mono, phasey sound.
        private static readonly int[] CombDelaysL = { 1557, 1617, 1491, 1422 };
        private static readonly int[] CombDelaysR = { 1617, 1557, 1422, 1491 };
        private static readonly int[] AllpassDelays = { 225, 556 };

        private const int NumCombs   = 4;
        private const int NumAllpass = 2;

        // Comb filter state — one set per channel (0 = L, 1 = R)
        private NativeArray<float> _combBuffersL;   // flattened: comb index * maxCombLen + sample
        private NativeArray<float> _combBuffersR;
        private NativeArray<int>   _combWritePosL;
        private NativeArray<int>   _combWritePosR;
        private NativeArray<float> _combFilterStore; // damping low-pass state, 8 values (4L + 4R)

        // Allpass filter state — one set per channel
        private NativeArray<float> _allpassBuffersL;
        private NativeArray<float> _allpassBuffersR;
        private NativeArray<int>   _allpassWritePosL;
        private NativeArray<int>   _allpassWritePosR;

        private int  _sampleRate;
        private int  _channelCounter;
        private bool _initialized;

        private float _roomSize; // mapped to comb feedback (~0.7–0.98)
        private float _damping;  // mapped to damp coefficient (~0.0–1.0)
        private float _wet;

        private AudioProcessorSettings.ReverbSettings.Unmanaged _settings;

        // Max buffer sizes (sized for 44100; scaled up if sample rate differs)
        private const int MaxCombLen   = 2048;
        private const int MaxAllpassLen = 1024;

        public AudioProcessorReverb(int sampleRate)
        {
            _sampleRate    = sampleRate > 0 ? sampleRate : 44100;
            _channelCounter = 0;
            _initialized   = false;
            _roomSize      = 0f;
            _damping       = 0f;
            _wet           = 0f;
            _settings      = new AudioProcessorSettings.ReverbSettings().ToUnmanaged();

            _combBuffersL    = default;
            _combBuffersR    = default;
            _combWritePosL   = default;
            _combWritePosR   = default;
            _combFilterStore = default;
            _allpassBuffersL = default;
            _allpassBuffersR = default;
            _allpassWritePosL = default;
            _allpassWritePosR = default;
        }

        void UpdateSettings()
        {
            // Map [0,1] roomSize → feedback range [0.70, 0.98] (classic Schroeder range)
            _roomSize = 0.70f + _settings.roomSize * 0.28f;
            _damping  = _settings.damping;
            _wet      = _settings.wet;

            if (_sampleRate == 0)
            {
                _sampleRate = AudioSettings.outputSampleRate;
                if (_sampleRate == 0) _sampleRate = 44100;
            }

            if (_initialized) return;

            // Scale delay lengths from 44100 base rate
            float scale = _sampleRate / 44100f;

            _combBuffersL    = new NativeArray<float>(NumCombs * MaxCombLen, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _combBuffersR    = new NativeArray<float>(NumCombs * MaxCombLen, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _combWritePosL   = new NativeArray<int>(NumCombs, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _combWritePosR   = new NativeArray<int>(NumCombs, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _combFilterStore = new NativeArray<float>(NumCombs * 2, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _allpassBuffersL  = new NativeArray<float>(NumAllpass * MaxAllpassLen, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _allpassBuffersR  = new NativeArray<float>(NumAllpass * MaxAllpassLen, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _allpassWritePosL = new NativeArray<int>(NumAllpass, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _allpassWritePosR = new NativeArray<int>(NumAllpass, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _initialized = true;
        }

        public void SetGate(bool gate) { }

        public void SetSettings(AudioProcessorSettings.Unmanaged settings)
        {
            _settings = settings.reverbSettings;
            UpdateSettings();
        }

        public void DoUpdate() { }

        public float Process(float sample, AnysongTrack anysongTrack)
        {
            UpdateSettings();
            if (!_initialized) return sample;

            int channel = _channelCounter % 2;
            _channelCounter++;

            float wet = channel == 0
                ? ProcessChannel(sample, channel,
                    _combBuffersL, _combWritePosL,
                    _allpassBuffersL, _allpassWritePosL,
                    CombDelaysL, storeOffset: 0)
                : ProcessChannel(sample, channel,
                    _combBuffersR, _combWritePosR,
                    _allpassBuffersR, _allpassWritePosR,
                    CombDelaysR, storeOffset: NumCombs);

            return (wet * _wet) + (sample * (1f - _wet));
        }

        // Processes one sample through comb bank → allpass chain for one channel.
        private float ProcessChannel(
            float input,
            int channel,
            NativeArray<float> combBuffers,
            NativeArray<int>   combWritePos,
            NativeArray<float> allpassBuffers,
            NativeArray<int>   allpassWritePos,
            int[]              combDelays,
            int                storeOffset)
        {
            float scale = _sampleRate / 44100f;
            float combOut = 0f;

            // --- 4 parallel comb filters (with damping low-pass) ---
            for (int i = 0; i < NumCombs; i++)
            {
                int len   = Mathf.Clamp(Mathf.RoundToInt(combDelays[i] * scale), 1, MaxCombLen);
                int wPos  = combWritePos[i];
                int rPos  = (wPos - len + MaxCombLen * 2) % len; // keep positive

                // Fix: read position within this comb's slice
                int sliceBase = i * MaxCombLen;
                float delayed = combBuffers[sliceBase + rPos];

                // Damping: one-pole low-pass on the feedback signal
                int   storeIdx = storeOffset + i;
                float store    = _combFilterStore[storeIdx];
                store          = delayed * (1f - _damping) + store * _damping;
                _combFilterStore[storeIdx] = store;

                combBuffers[sliceBase + wPos] = input + store * _roomSize;
                combWritePos[i] = (wPos + 1) % len;

                combOut += delayed;
            }

            combOut *= 0.25f; // average the 4 combs

            // --- 2 series allpass filters ---
            // Allpass: y[n] = -g*x[n] + x[n-D] + g*y[n-D]  (feedback coefficient g = 0.5)
            const float g = 0.5f;
            float apIn = combOut;

            for (int i = 0; i < NumAllpass; i++)
            {
                int len  = Mathf.Clamp(Mathf.RoundToInt(AllpassDelays[i] * scale), 1, MaxAllpassLen);
                int wPos = allpassWritePos[i];
                int rPos = (wPos - len + MaxAllpassLen * 2) % len;

                int   sliceBase = i * MaxAllpassLen;
                float bufOut    = allpassBuffers[sliceBase + rPos];
                float bufIn     = apIn + bufOut * g;

                allpassBuffers[sliceBase + wPos] = bufIn;
                allpassWritePos[i] = (wPos + 1) % len;

                apIn = bufOut - g * bufIn; // allpass output
            }

            return apIn;
        }

        public void Dispose()
        {
            if (_combBuffersL.IsCreated)    _combBuffersL.Dispose();
            if (_combBuffersR.IsCreated)    _combBuffersR.Dispose();
            if (_combWritePosL.IsCreated)   _combWritePosL.Dispose();
            if (_combWritePosR.IsCreated)   _combWritePosR.Dispose();
            if (_combFilterStore.IsCreated) _combFilterStore.Dispose();

            if (_allpassBuffersL.IsCreated)   _allpassBuffersL.Dispose();
            if (_allpassBuffersR.IsCreated)   _allpassBuffersR.Dispose();
            if (_allpassWritePosL.IsCreated)  _allpassWritePosL.Dispose();
            if (_allpassWritePosR.IsCreated)  _allpassWritePosR.Dispose();

            _initialized = false;
        }
    }
}