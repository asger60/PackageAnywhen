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
                LFO,
                Envelope,
            }

            public ModSources modSource;
            [Range(0, 1f)] public float modAmount;
            [DynamicRange] public DynamicRangeFloat modDepth;

            private SynthControlBase _modSourceControl;

            public ModRouting(ModSources modSource, float modAmount, DynamicRangeFloat modDepth)
            {
                this.modSource = modSource;
                this.modAmount =modAmount;
                this.modDepth = modDepth;
                _modSourceControl = null;
            }

            public struct Unmanaged
            {
                public ModSources modSource;
                public float modAmount;
                public DynamicRangeFloat modDepth;

                public Unmanaged(ModSources modSource, float modAmount, DynamicRangeFloat modDepth)
                {
                    this.modSource = modSource;
                    this.modAmount = modAmount;
                    this.modDepth = modDepth;
                }
            }

            public Unmanaged ToUnmanaged()
            {
                return new Unmanaged(modSource, modAmount, modDepth);
            }

            public void Set(AnywhenPlayerBase.PlayerTrack track)
            {
                _modSourceControl = modSource == ModSources.LFO ? track.trackLFO : track.trackEnvelope;
            }


            public float Process(float input)
            {
                switch (modSource)
                {
                    case ModSources.LFO:
                        return input + (_modSourceControl.Process(true) * modDepth.value);

                    case ModSources.Envelope:
                        return input * Mathf.Lerp(1, _modSourceControl.Process(), modAmount);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public SynthSettingsObjectFilter Settings { get; protected set; }
        protected List<ModRouting> ModRoutings = new();

        public void AddModRouting(ModRouting modRouting)
        {
            ModRoutings.Add(modRouting);
        }

        protected virtual void UpdateSettings()
        {
        }

        public abstract void SetExpression(float data);

        public abstract void SetSettings(SynthSettingsObjectFilter newSettings);


        public abstract void HandleModifiers(float mod1);


        public virtual float Process(float sample)
        {
            return sample;
        }
    }
}