using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Synth.Filter
{
    public abstract class SynthFilterBase
    {
        [Serializable]
        public struct ModRouting
        {
            public enum ModSources
            {
                LFO1,
                Envelope,
            }

            public ModSources modSource;
            [Range(0, 1f)] public float modAmount;

            // private SynthControlBase _modSourceControl;

            public ModRouting(ModSources modSource, float modAmount)
            {
                this.modSource = modSource;
                this.modAmount = modAmount;
                //  _modSourceControl = null;
            }

            public struct Unmanaged
            {
                public ModSources modSource;
                public float modAmount;

                public Unmanaged(ModSources modSource, float modAmount)
                {
                    this.modSource = modSource;
                    this.modAmount = modAmount;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged(modSource, modAmount);
            }

            public void Set(AnywhenPlayerBase.PlayerTrack track)
            {
                Debug.LogWarning("Not implemented yet");
                // _modSourceControl = modSource == ModSources.LFO ? track.trackLFO : track.trackEnvelope;
            }


            public float Process(float input)
            {
                return 0;
                //switch (modSource)
                //{
                //    case ModSources.LFO:
                //        return input + (_modSourceControl.Process(true) * modDepth.value);
//
                //    case ModSources.Envelope:
                //        return input * Mathf.Lerp(1, _modSourceControl.Process(), modAmount);
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
            }
        }

        public AudioProcessorSettingsObject Settings { get; protected set; }
        protected List<ModRouting> ModRoutings = new();

        public void AddModRouting(ModRouting modRouting)
        {
            ModRoutings.Add(modRouting);
        }

        protected virtual void UpdateSettings()
        {
        }

        public abstract void SetExpression(float data);

        public abstract void SetSettings(AudioProcessorSettingsObject newSettings);


        public abstract void HandleModifiers(float mod1);


        public virtual float Process(float sample)
        {
            return sample;
        }
    }
}