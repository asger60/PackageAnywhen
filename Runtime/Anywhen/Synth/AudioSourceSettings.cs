using System;
using Anywhen.SettingsObjects;
using Unity.GraphToolkit.Editor;
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
            public float sourceVolume;
            
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
            public struct Unmanaged
            {
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
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