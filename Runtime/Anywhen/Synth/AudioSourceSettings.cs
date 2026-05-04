using System;
using Unity.GraphToolkit.Editor;

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
            public SampleSourceSettings sampleSourceSettings;
            public SynthSourceSettings synthSourceSettings;
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                audioSourceType = audioSourceType,
                sampleSourceSettings = sampleSourceSettings,
                synthSourceSettings = synthSourceSettings,
            };
        }
    }
}