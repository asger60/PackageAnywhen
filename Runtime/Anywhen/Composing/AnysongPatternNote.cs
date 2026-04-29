using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    [Serializable]
    public struct AnysongPatternNote
    {
        [Range(0.01f, 2f)] public float duration;
        [Range(0f, 1f)] public float drift;
        [Range(0, 1f)] public float velocity;
        [Range(0, 1f)] public float mixWeight;
        [Range(0, 4)] public int stepRepeats;
        [Range(0, 1f)] public float chance;
        public int noteIndex;

        public enum RepeatRates
        {
            ThirtyTwo,
            FourtyEight,
            SixtyFour
        }

        public RepeatRates repeatRate;

        public AnysongPatternNote(int noteIndex)
        {
            duration = AnywhenAudioMetronome.Sub16Length;
            drift = 0;
            velocity = 1;
            mixWeight = 1;
            stepRepeats = 0;
            chance = 1;
            this.noteIndex = noteIndex;
            repeatRate = RepeatRates.ThirtyTwo;
        }


        public AnysongPatternNote Clone()
        {
            var clone = (AnysongPatternNote)MemberwiseClone();
            return clone;
        }

        public bool IsNull()
        {
            return (duration == 0 && drift == 0 && velocity == 0 && mixWeight == 0);
        }

        public bool Equals(AnysongPatternNote other)
        {
            return other.noteIndex == noteIndex && Mathf.Approximately(other.duration, duration) &&
                   Mathf.Approximately(other.drift, drift) &&
                   Mathf.Approximately(other.velocity, velocity) && Mathf.Approximately(other.mixWeight, mixWeight) &&
                   other.stepRepeats == stepRepeats &&
                   Mathf.Approximately(other.chance, chance);
        }
    }
}