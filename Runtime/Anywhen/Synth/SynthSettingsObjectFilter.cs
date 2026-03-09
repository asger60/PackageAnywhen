using System;
using UnityEngine;


    public class SynthSettingsObjectFilter : SynthSettingsObjectBase
    {
        public enum FilterTypes
        {
            LowPass,
            BandPass,
            Formant,
            Ladder
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
            [Range(10, 100)] public float bandWidth;
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

            bandPassSettings.bandWidth = 10;
            bandPassSettings.frequency = 1000;

            formantSettings.vowel = 1;
        }
    }
