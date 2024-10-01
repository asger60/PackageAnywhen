using Anywhen.Synth.Filter;
using UnityEngine;
using UnitySynth.Runtime.Synth;

namespace Anywhen.Synth
{
    public class SynthFilterFormant : SynthFilterBase
    {
        private SynthFilterBandPass _synthFilterBandPass1;
        private SynthFilterBandPass _synthFilterBandPass2;
        private SynthFilterBandPass _synthFilterBandPass3;

        struct FormantBand
        {
            public float frequency;
            public float q;
            public float gain;


            public FormantBand(float frequency, float bw, float gainDB)
            {
                this.frequency = frequency;
                float halfWidth = bw / 2f;
                this.q = frequency / ((frequency + halfWidth) - (frequency - halfWidth));
                gain = DecibelToLinear(gainDB);
            }
        }

        static float DecibelToLinear(float dB)
        {
            float linear = Mathf.Pow(10.0f, dB / 20.0f);
            return linear;
        }

        readonly struct FormantVowel
        {
            private readonly string name;
            private readonly FormantBand[] _formantBands;

            public FormantBand GetBand(int i)
            {
                return _formantBands[i];
            }

            public FormantVowel(string name, FormantBand[] bands)
            {
                this.name = name;
                _formantBands = bands;
            }
        }

        // vowels from here: http://www.csounds.com/manual/html/MiscFormants.html
        private readonly FormantVowel[] _vowels =
        {
            new(
                "EEE",
                new[]
                {
                    new FormantBand(400, 60, 1),
                    new FormantBand(1600, 80, -24f),
                    new FormantBand(2700, 120, -30f)
                }),
            new(
                "III",
                new[]
                {
                    new FormantBand(350, 50, 0),
                    new FormantBand(1700, 100, -20f),
                    new FormantBand(2700, 120, -30f)
                }),
            new(
                "OOO",
                new[]
                {
                    new FormantBand(450, 80f, 0),
                    new FormantBand(800, 80f, -9f),
                    new FormantBand(2830, 100f, -16f)
                }),
            new(
                "AAA",
                new[]
                {
                    new FormantBand(600, 60, 0),
                    new FormantBand(1040, 70, -7f),
                    new FormantBand(2250, 110, -9f)
                }),
            new(
                "EEE",
                new[]
                {
                    new FormantBand(400, 40, 0),
                    new FormantBand(1620, 80, -12f),
                    new FormantBand(2400, 100, -9f)
                }),
            new(
                "III",
                new[]
                {
                    new FormantBand(250, 60, 0),
                    new FormantBand(1750, 80, -30f),
                    new FormantBand(2600, 100, -16f)
                }),
            new(
                "OOO",
                new[]
                {
                    new FormantBand(400, 40, 0),
                    new FormantBand(750, 80, -11f),
                    new FormantBand(2400, 100, -21f)
                }),
            new(
                "UUU",
                new[]
                {
                    new FormantBand(350, 40, 0),
                    new FormantBand(600, 80, -20f),
                    new FormantBand(2400, 100, -32f)
                }),
        };

        private FormantVowel _currentVowel;

        public override void SetSettings(SynthSettingsObjectFilter newSettings)
        {
            settings = newSettings;
            Init(AnywhenRuntime.SampleRate);
        }


        private void Init(int sampleRate)
        {
            _sampleRate = sampleRate;

            _synthFilterBandPass1 = gameObject.AddComponent<SynthFilterBandPass>();
            _synthFilterBandPass2 = gameObject.AddComponent<SynthFilterBandPass>();
            _synthFilterBandPass3 = gameObject.AddComponent<SynthFilterBandPass>();
            _currentVowel = _vowels[3];

            _synthFilterBandPass1.Init(sampleRate);
            _synthFilterBandPass2.Init(sampleRate);
            _synthFilterBandPass3.Init(sampleRate);


            _synthFilterBandPass1.SetFrequency(_currentVowel.GetBand(0).frequency);
            _synthFilterBandPass1.SetQ(_currentVowel.GetBand(0).q);

            _synthFilterBandPass2.SetFrequency(_currentVowel.GetBand(1).frequency);
            _synthFilterBandPass2.SetQ(_currentVowel.GetBand(1).q);

            _synthFilterBandPass3.SetFrequency(_currentVowel.GetBand(2).frequency);
            _synthFilterBandPass3.SetQ(_currentVowel.GetBand(2).q);
        }

        private void SetVowel(int index)
        {
            _currentVowel = _vowels[index];
            _synthFilterBandPass1.SetFrequency(_currentVowel.GetBand(0).frequency);
            _synthFilterBandPass1.SetQ(_currentVowel.GetBand(0).q);
            _synthFilterBandPass2.SetFrequency(_currentVowel.GetBand(1).frequency);
            _synthFilterBandPass2.SetQ(_currentVowel.GetBand(1).q);
            _synthFilterBandPass3.SetFrequency(_currentVowel.GetBand(2).frequency);
            _synthFilterBandPass3.SetQ(_currentVowel.GetBand(2).q);
        }

        float _sampleRate = 48000; // Sample rate

        public override void SetExpression(float data)
        {
        }


        public override void SetParameters(SynthSettingsObjectFilter settingsObjectFilter)
        {
            SetVowel(settingsObjectFilter.formantSettings.vowel);
        }

        public override void HandleModifiers(float mod1)
        {
        }



        public override float Process(float sample)
        {
            //float mix1 = sample;
            //float mix2 = sample;
            //float mix3 = sample;
            //samples.CopyTo(mix1, 0);
            //samples.CopyTo(mix2, 0);
            //samples.CopyTo(mix3, 0);


            float mix1 = _synthFilterBandPass1.Process(sample);
            float mix2 = _synthFilterBandPass2.Process(sample);
            float mix3 = _synthFilterBandPass3.Process(sample);

            //int idx = offset;
            //for (int i = 0; i < sampleCount; ++i)

            sample = ((mix1 * _currentVowel.GetBand(0).gain) +
                      (mix2 * _currentVowel.GetBand(1).gain) +
                      (mix3 * _currentVowel.GetBand(2).gain)) / 3f;
            //  idx += stride;

            return sample;
        }
    }
}