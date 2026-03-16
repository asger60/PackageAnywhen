using System;
using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    public class SynthSettingsObjectFilter : SynthSettingsObjectBase
    {
        public SynthFilterBase.ModRouting[] modRouting;

        public enum FilterTypes
        {
            LowPassFilter,
            BandPassFilter,
            FormantFilter,
            LadderFilter
        }

        public FilterTypes filterType;

        [Serializable]
        public struct LowPassSettings
        {
            [Range(1, 4)] public int oversampling;
            [Range(10, 24000)] public float cutoffFrequency;
            [Range(0, 1)] public float resonance;
        }

        public LowPassSettings lowPassSettings;

        [Serializable]
        public struct LadderSettings
        {
            [Range(1, 4)] public int oversampling;
            [Range(10, 24000)] public float cutoffFrequency;
            [Range(0, 1)] public float resonance;
        }

        public LadderSettings ladderSettings;

        [Serializable]
        public struct BandPassSettings
        {
            [Range(10, 24000)] public float frequency;
            [Range(1, 10000)] public float bandWidth;
            [Range(0.01f, 100)] public float q;
        }

        public BandPassSettings bandPassSettings;

        [Serializable]
        public struct FormantSettings
        {
            [Range(1, 6)] public int vowel;
        }

        public FormantSettings formantSettings;

        public void Init()
        {
            lowPassSettings.oversampling = 2;
            lowPassSettings.cutoffFrequency = 24000;
            lowPassSettings.resonance = 0.25f;

            ladderSettings.oversampling = 2;
            ladderSettings.cutoffFrequency = 24000;
            ladderSettings.resonance = 0.25f;

            bandPassSettings.bandWidth = 100;
            bandPassSettings.frequency = 1000;
            bandPassSettings.q = 10;

            formantSettings.vowel = 1;
        }

        public void SyncBandPassFromQ()
        {
            bandPassSettings.frequency = Mathf.Clamp(bandPassSettings.frequency, 10f, 24000f);
            bandPassSettings.q = Mathf.Max(bandPassSettings.q, 0.01f);
            bandPassSettings.bandWidth = bandPassSettings.frequency / bandPassSettings.q;
        }

        public void SyncBandPassFromBandwidth()
        {
            bandPassSettings.frequency = Mathf.Clamp(bandPassSettings.frequency, 10f, 24000f);
            bandPassSettings.bandWidth = Mathf.Max(bandPassSettings.bandWidth, 1f);
            bandPassSettings.q = bandPassSettings.frequency / bandPassSettings.bandWidth;
        }
    }
}