using System;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;

namespace Anywhen.Synth
{
    [Serializable]
    public class AudioProcessorSettings
    {
        public enum FilterTypes
        {
            LowPassFilter,
            BandPassFilter,
            FormantFilter,
            LadderFilter,
            BitcrushFilter,
            SaturatorFilter,
            DelayFilter,
            ChorusFilter,
            ReverbFilter
        }

        public FilterTypes filterType;

        [Serializable]
        public struct LowPassSettings
        {
            [Range(1, 4)] public int oversampling;
            [Range(10, 24000)] public float cutoffFrequency;
            [Range(0, 1)] public float resonance;
            public SynthFilterBase.ModRouting[] cutoffMod;

            public struct Unmanaged
            {
                public float CutoffFrequency;
                public float Resonance;
                public int Oversampling;
                public NativeArray<SynthFilterBase.ModRouting> CutoffMod;

                public void Dispose()
                {
                    if (CutoffMod.IsCreated) CutoffMod.Dispose();
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(CutoffFrequency, other.CutoffFrequency) && Mathf.Approximately(Resonance, other.Resonance) &&
                           Oversampling == other.Oversampling;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    CutoffFrequency = cutoffFrequency,
                    Resonance = resonance,
                    Oversampling = oversampling,
                    CutoffMod = new NativeArray<SynthFilterBase.ModRouting>(
                        cutoffMod ?? Array.Empty<SynthFilterBase.ModRouting>(), Allocator.Persistent)
                };
            }
        }

        public LowPassSettings lowPassSettings;

        [Serializable]
        public struct LadderSettings
        {
            [Range(1, 4)] public int oversampling;
            [Range(10, 24000)] public float cutoffFrequency;
            [Range(0, 1)] public float resonance;
            public SynthFilterBase.ModRouting[] cutoffMod;

            public struct Unmanaged
            {
                public float cutoffFrequency;
                public float resonance;
                public int oversampling;
                public NativeArray<SynthFilterBase.ModRouting> cutoffMod;

                public void Dispose()
                {
                    if (cutoffMod.IsCreated) cutoffMod.Dispose();
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(cutoffFrequency, other.cutoffFrequency) && Mathf.Approximately(resonance, other.resonance) &&
                           oversampling == other.oversampling;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    cutoffFrequency = cutoffFrequency,
                    resonance = resonance,
                    oversampling = oversampling,
                    cutoffMod = new NativeArray<SynthFilterBase.ModRouting>(cutoffMod ?? Array.Empty<SynthFilterBase.ModRouting>(),
                        Allocator.Persistent
                    ),
                };
            }
        }

        public LadderSettings ladderSettings;

        [Serializable]
        public struct BandPassSettings
        {
            [Range(10, 24000)] public float frequency;
            [Range(1, 10000)] public float bandWidth;
            [Range(0.01f, 100)] public float q;

            public struct Unmanaged
            {
                public float frequency;
                public float bandWidth;
                public float q;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(frequency, other.frequency) && Mathf.Approximately(bandWidth, other.bandWidth) &&
                           Mathf.Approximately(q, other.q);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    frequency = frequency,
                    bandWidth = bandWidth,
                    q = q,
                };
            }
        }

        public BandPassSettings bandPassSettings;

        [Serializable]
        public struct FormantSettings
        {
            [Range(1, 6)] public int vowel;

            public struct Unmanaged
            {
                public int vowel;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return vowel == other.vowel;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    vowel = vowel,
                };
            }
        }

        public FormantSettings formantSettings;

        [Serializable]
        public struct BitcrushSettings
        {
            [Range(1, 24)] public float bitDepth;
            [Range(1, 100)] public int downsampling;

            public struct Unmanaged
            {
                public float bitDepth;
                public int downsampling;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(bitDepth, other.bitDepth) && downsampling == other.downsampling;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    bitDepth = bitDepth,
                    downsampling = downsampling,
                };
            }
        }

        public BitcrushSettings bitcrushSettings;

        [Serializable]
        public struct SaturatorSettings
        {
            [Range(0, 10)] public float drive;
            [Range(0, 1)] public float wet;

            public struct Unmanaged
            {
                public float drive;
                public float wet;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(drive, other.drive) && Mathf.Approximately(wet, other.wet);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    drive = drive,
                    wet = wet,
                };
            }
        }

        public SaturatorSettings saturatorSettings;

        [Serializable]
        public struct DelaySettings
        {
            [Range(0, 1)] public float delayTime;
            [Range(0, 1)] public float feedback;
            [Range(0, 1)] public float wet;

            public struct Unmanaged
            {
                public float delayTime;
                public float feedback;
                public float wet;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(delayTime, other.delayTime) && Mathf.Approximately(feedback, other.feedback) &&
                           Mathf.Approximately(wet, other.wet);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    delayTime = delayTime,
                    feedback = feedback,
                    wet = wet,
                };
            }
        }

        public DelaySettings delaySettings;
// Add to FilterTypes enum:
// ReverbFilter

        [Serializable]
        public struct ReverbSettings
        {
            [Range(0f, 1f)] public float roomSize; // Controls comb filter feedback
            [Range(0f, 1f)] public float damping; // High-frequency rolloff in comb filters
            [Range(0f, 1f)] public float wet;

            public struct Unmanaged
            {
                public float roomSize;
                public float damping;
                public float wet;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(roomSize, other.roomSize) && Mathf.Approximately(damping, other.damping) &&
                           Mathf.Approximately(wet, other.wet);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    roomSize = roomSize,
                    damping = damping,
                    wet = wet,
                };
            }
        }

        public ReverbSettings reverbSettings;

        [Serializable]
        public struct ChorusSettings
        {
            [Range(0, 1)] public float rate;
            [Range(0, 1)] public float depth;
            [Range(0, 1)] public float delay;
            [Range(0, 1)] public float feedback;
            [Range(0, 1)] public float wet;

            public struct Unmanaged
            {
                public float rate;
                public float depth;
                public float delay;
                public float feedback;
                public float wet;

                public void Dispose()
                {
                }

                public bool Equals(Unmanaged other)
                {
                    return Mathf.Approximately(rate, other.rate) && Mathf.Approximately(depth, other.depth) &&
                           Mathf.Approximately(delay, other.delay) && Mathf.Approximately(feedback, other.feedback) &&
                           Mathf.Approximately(wet, other.wet);
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged
                {
                    rate = rate,
                    depth = depth,
                    delay = delay,
                    feedback = feedback,
                    wet = wet,
                };
            }
        }

        public ChorusSettings chorusSettings;

        [Serializable]
        public struct EnvelopeSettings : IEquatable<EnvelopeSettings>
        {
            public bool enabled;
            [Range(0, 2f)] public float attack;
            [Range(0, 1f)] public float decay;
            [Range(0, 1f)] public float sustain;
            [Range(0, 3f)] public float release;


            public EnvelopeSettings(float attack, float decay, float sustain, float release, bool enabled) : this()
            {
                this.enabled = enabled;
                this.attack = attack;
                this.decay = decay;
                this.sustain = sustain;
                this.release = release;
            }

            public bool IsUnset()
            {
                return attack == 0 && decay == 0 && sustain == 0 && release == 0;
            }

            public void Initialize()
            {
                attack = 0.01f;
                decay = 0.1f;
                sustain = 0.5f;
                release = 0.1f;
            }

            public bool Equals(EnvelopeSettings other)
            {
                return enabled == other.enabled &&
                       Mathf.Approximately(attack, other.attack) &&
                       Mathf.Approximately(decay, other.decay) &&
                       Mathf.Approximately(sustain, other.sustain) &&
                       Mathf.Approximately(release, other.release);
            }

            public override bool Equals(object obj)
            {
                return obj is EnvelopeSettings other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = enabled.GetHashCode();
                    hashCode = (hashCode * 397) ^ attack.GetHashCode();
                    hashCode = (hashCode * 397) ^ decay.GetHashCode();
                    hashCode = (hashCode * 397) ^ sustain.GetHashCode();
                    hashCode = (hashCode * 397) ^ release.GetHashCode();
                    return hashCode;
                }
            }
        }

        public EnvelopeSettings envelopeSettings;


        [Serializable]
        public struct LFOSettings : IEquatable<LFOSettings>
        {
            public bool enabled;
            [Range(0.01f, 10)] public float frequency;
            public bool unipolar;


            public LFOSettings(float frequency, float amplitude, bool enabled) : this()
            {
                this.enabled = enabled;
                this.frequency = frequency;
            }

            public bool IsUnset()
            {
                return frequency == 0;
            }

            public void Initialize()
            {
                frequency = 1;
                unipolar = false;
            }

            public bool Equals(LFOSettings other)
            {
                return enabled == other.enabled &&
                       Mathf.Approximately(frequency, other.frequency) &&
                       unipolar == other.unipolar;
            }

            public override bool Equals(object obj)
            {
                return obj is LFOSettings other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = enabled.GetHashCode();
                    hashCode = (hashCode * 397) ^ frequency.GetHashCode();
                    hashCode = (hashCode * 397) ^ unipolar.GetHashCode();
                    return hashCode;
                }
            }
        }

        public LFOSettings lfoSettings;


        public struct Unmanaged : IEquatable<Unmanaged>
        {
            public FilterTypes filterType;
            public LowPassSettings.Unmanaged lowPassSettings;
            public LadderSettings.Unmanaged ladderSettings;
            public BandPassSettings.Unmanaged bandPassSettings;
            public FormantSettings.Unmanaged formantSettings;
            public BitcrushSettings.Unmanaged bitcrushSettings;
            public SaturatorSettings.Unmanaged saturatorSettings;
            public DelaySettings.Unmanaged delaySettings;
            public ChorusSettings.Unmanaged chorusSettings;
            public EnvelopeSettings envelopeSettings;
            public LFOSettings lfoSettings;
            public ReverbSettings.Unmanaged reverbSettings;

            public void Dispose()
            {
                lowPassSettings.Dispose();
                ladderSettings.Dispose();
                bandPassSettings.Dispose();
                formantSettings.Dispose();
                bitcrushSettings.Dispose();
                saturatorSettings.Dispose();
                delaySettings.Dispose();
                chorusSettings.Dispose();
                reverbSettings.Dispose();
            }

            public bool Equals(Unmanaged other)
            {
                return filterType == other.filterType && lowPassSettings.Equals(other.lowPassSettings) &&
                       ladderSettings.Equals(other.ladderSettings) && bandPassSettings.Equals(other.bandPassSettings) &&
                       formantSettings.Equals(other.formantSettings) && bitcrushSettings.Equals(other.bitcrushSettings) &&
                       saturatorSettings.Equals(other.saturatorSettings) && delaySettings.Equals(other.delaySettings) &&
                       chorusSettings.Equals(other.chorusSettings) && envelopeSettings.Equals(other.envelopeSettings) &&
                       lfoSettings.Equals(other.lfoSettings) && reverbSettings.Equals(other.reverbSettings);
            }

            public override bool Equals(object obj)
            {
                return obj is Unmanaged other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add((int)filterType);
                hashCode.Add(lowPassSettings);
                hashCode.Add(ladderSettings);
                hashCode.Add(bandPassSettings);
                hashCode.Add(formantSettings);
                hashCode.Add(bitcrushSettings);
                hashCode.Add(saturatorSettings);
                hashCode.Add(delaySettings);
                hashCode.Add(chorusSettings);
                hashCode.Add(envelopeSettings);
                hashCode.Add(lfoSettings);
                hashCode.Add(reverbSettings);
                return hashCode.ToHashCode();
            }
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                filterType = filterType,
                lowPassSettings = lowPassSettings.ToUnmanaged(),
                ladderSettings = ladderSettings.ToUnmanaged(),
                bandPassSettings = bandPassSettings.ToUnmanaged(),
                formantSettings = formantSettings.ToUnmanaged(),
                bitcrushSettings = bitcrushSettings.ToUnmanaged(),
                saturatorSettings = saturatorSettings.ToUnmanaged(),
                delaySettings = delaySettings.ToUnmanaged(),
                chorusSettings = chorusSettings.ToUnmanaged(),
                envelopeSettings = envelopeSettings,
                lfoSettings = lfoSettings,
                reverbSettings = reverbSettings.ToUnmanaged()
            };
        }

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

            bitcrushSettings.bitDepth = 16f;
            bitcrushSettings.downsampling = 1;

            saturatorSettings.drive = 1f;
            saturatorSettings.wet = 1f;

            delaySettings.delayTime = 0.5f;
            delaySettings.feedback = 0.5f;
            delaySettings.wet = 0.5f;

            chorusSettings.rate = 0.1f;
            chorusSettings.depth = 0.2f;
            chorusSettings.delay = 0.5f;
            chorusSettings.feedback = 0.3f;
            chorusSettings.wet = 0.5f;

            reverbSettings.roomSize = 0.5f;
            reverbSettings.damping = 0.5f;
            reverbSettings.wet = 0.4f;
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