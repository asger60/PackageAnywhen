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
            Noise,
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
                public bool Equals(Unmanaged other)
                {
                    return SampleInstrument.Equals(other.SampleInstrument) && 
                        Mathf.Approximately(SourceVolume, other.SourceVolume);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                

                return new Unmanaged
                {
                    SampleInstrument = !sampleInstrument ? default : sampleInstrument.ToUnmanaged(),
                    SourceVolume = sourceVolume,
                };
            }

            public bool IsNull()
            {
                if(sampleInstrument == null)
                    return true;
                return false;
            }
        }

        public SampleSourceSettings sampleSourceSettings;


        [Serializable]
        public struct SynthSourceSettings
        {
            [Range(0, 1)] public float sourceVolume;
            [Range(-24, 24)] public int noteOffset;
            public float detune;

            public enum SynthType
            {
                Sine,
                Saw,
                Square,
            }

            public SynthType synthType;

            public struct Unmanaged
            {
                public SynthType SynthType;

                [Range(0, 1)] public float SourceVolume;
                [Range(-24, 24)] public int NoteOffset;
                public float Detune;
                public bool Equals(Unmanaged other)
                {
                    return SynthType == other.SynthType && 
                        Mathf.Approximately(SourceVolume, other.SourceVolume) && 
                        NoteOffset == other.NoteOffset && 
                        Mathf.Approximately(Detune, other.Detune);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    SourceVolume = sourceVolume,
                    SynthType = synthType,
                    NoteOffset = noteOffset,
                    Detune = detune,
                };
            }
        }

        public SynthSourceSettings synthSourceSettings;

        [Serializable]
        public struct NoiseSourceSettings
        {
            [Range(0, 1)] public float sourceVolume;

            public enum NoiseType
            {
                White,
                Pink,
                Brown,
            }

            public NoiseType noiseType;

            public struct Unmanaged
            {
                public NoiseType NoiseType;
                [Range(0, 1)] public float SourceVolume;
                public bool Equals(Unmanaged other)
                {
                    return NoiseType == other.NoiseType && 
                        Mathf.Approximately(SourceVolume, other.SourceVolume);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    SourceVolume = sourceVolume,
                    NoiseType = noiseType,
                };
            }
        }

        public NoiseSourceSettings noiseSourceSettings;

        public void Init()
        {
        }


        public struct Unmanaged
        {
            public AudioSourceTypes audioSourceType;
            public SampleSourceSettings.Unmanaged sampleSourceSettings;
            public SynthSourceSettings.Unmanaged synthSourceSettings;
            public NoiseSourceSettings.Unmanaged noiseSourceSettings;
            
            public bool Equals(Unmanaged other)
            {
                return audioSourceType == other.audioSourceType &&
                    sampleSourceSettings.Equals(other.sampleSourceSettings) &&
                    synthSourceSettings.Equals(other.synthSourceSettings) &&
                    noiseSourceSettings.Equals(other.noiseSourceSettings);
            }
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                audioSourceType = audioSourceType,
                sampleSourceSettings = sampleSourceSettings.ToUnmanaged(),
                synthSourceSettings = synthSourceSettings.ToUnmanaged(),
                noiseSourceSettings = noiseSourceSettings.ToUnmanaged(),
            };
        }
    }
}