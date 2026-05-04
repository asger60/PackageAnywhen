using System;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.Synth
{
    [Serializable]
    public class AudioSourceSettings
    {
        public enum AudioSourceTypes
        {
            Sample,
            Synth,
        }


        public AudioSourceTypes audioSourceType;

        [Serializable]
        public struct SampleSourceSettings
        {
            public AnywhenSampleInstrument sampleInstrument;
            [Range(0, 1)] public float sourceVolume;

            public struct Unmanaged
            {
                public AnywhenSampleInstrument.Unmanaged SampleInstrument;
                [Range(0, 1)] public float SourceVolume;
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    SampleInstrument = sampleInstrument.ToUnmanaged(),
                    SourceVolume = sourceVolume,
                };
            }
        }

        public SampleSourceSettings sampleSourceSettings;


        [Serializable]
        public struct SynthSourceSettings
        {
            [Range(0, 1)] public float sourceVolume;

            public enum SynthType
            {
                Sine,
                Saw,
                Square,
            }

            public SynthType synthType;

            public struct Unmanaged
            {
                [Range(0, 1)] public float SourceVolume;
                public SynthType SynthType;
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    SourceVolume = sourceVolume,
                    SynthType = synthType,
                };
            }
        }

        public SynthSourceSettings synthSourceSettings;

        public void Init()
        {
        }


        public struct Unmanaged
        {
            public AudioSourceTypes audioSourceType;
            public SampleSourceSettings.Unmanaged sampleSourceSettings;
            public SynthSourceSettings.Unmanaged synthSourceSettings;
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                audioSourceType = audioSourceType,
                sampleSourceSettings = sampleSourceSettings.ToUnmanaged(),
                synthSourceSettings = synthSourceSettings.ToUnmanaged(),
            };
        }
    }
}