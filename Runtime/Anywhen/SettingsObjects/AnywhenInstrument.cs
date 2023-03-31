using System;
using Rytmos.AudioSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PackageAnywhen.Runtime.Anywhen
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenInstrument : AnywhenSettingsBase
    {
        public AudioClip[] audioClips;

        public float stopDuration = 0.1f;

        public enum InstrumentType
        {
            OneShotShort = 0,
            OneShotLong = 1,
            Sustained = 2
        }

        public InstrumentType instrumentType;

        public enum ClipSelectType
        {
            PitchedNotes,
            RandomVariations
        }

        public ClipSelectType clipSelectType;


        public AudioClip GetAudioClip(int note)
        {
            switch (clipSelectType)
            {
                case ClipSelectType.PitchedNotes:
                    note = AnywhenConductor.Instance.GetScaledNote(note);
                    if (note >= audioClips.Length) Debug.LogWarning("note out of clip range");
                    return note >= audioClips.Length ? null : audioClips[note];
                
                case ClipSelectType.RandomVariations:
                    return audioClips[Random.Range(0, audioClips.Length)];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}