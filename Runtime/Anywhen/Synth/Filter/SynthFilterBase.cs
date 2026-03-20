using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Synth.Filter
{
    public abstract class SynthFilterBase
    {
        [Serializable]
        public class ModRouting
        {
            public enum ModSources
            {
                LFO,
                Envelope,
            }

            public ModSources modSource;
            [Range(0, 1f)] public float modAmount;
            [DynamicRange] public DynamicRangeFloat modDepth = new (0,10);

            private SynthControlBase _modSourceControl;

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

        public abstract void SetExpression(float data);

        public abstract void SetSettings(SynthSettingsObjectFilter newSettings);

        public abstract void SetParameters(SynthSettingsObjectFilter settingsObjectFilter);

        public abstract void HandleModifiers(float mod1);


        public virtual float Process(float sample)
        {
            return sample;
        }
    }
}