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
                LFO2,
                Envelope1,
                Envelope2,
            }

            public ModSources modSource;
            [Range(0, 100f)] public float modAmount;


            public ModRouting(ModSources modSource, float modAmount)
            {
                this.modSource = modSource;
                this.modAmount = modAmount;
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