using System;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrack
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        public AnywhenSampleInstrument.EnvelopeSettings trackEnvelope;
        public bool enablePitchLFO;
        [Range(1, 100)] public float pitchLFOFrequency;
        [Range(0, 100)] public float pitchLFOAmplitude;

        public AnimationCurve intensityMappingCurve =
            new(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        //public bool monophonic;
        [Range(1, 8)] public int voices = 4;
        
        [NonSerialized] public bool IsMuted = false;
        [NonSerialized] public bool IsSolo = false;

        public enum AnyTrackTypes
        {
            None = 0,
            Bass = 10,
            NoteShort = 35,
            NoteLong = 36,
            [InspectorName("Rhythm/Hihat")] Hihat = 40,
            [InspectorName("Rhythm/Kick")] Kick = 50,
            [InspectorName("Rhythm/Snare")] Snare = 60,
            [InspectorName("Rhythm/Clap")] Clap = 70,
            [InspectorName("Rhythm/Tick")] Tick = 80,
            [InspectorName("Rhythm/Tom")] Tom = 90,
        }

        public AnyTrackTypes trackType;

        public void Init()
        {
            volume = 1;
            intensityMappingCurve = new AnimationCurve(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });
            trackEnvelope = new AnywhenSampleInstrument.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
        }

        public AnysongTrack Clone()
        {
            var clone = new AnysongTrack
            {
                instrument = instrument,
                volume = volume,
                intensityMappingCurve = intensityMappingCurve,
                trackEnvelope = trackEnvelope,
                trackType = trackType,
            };
            return clone;
        }


        public void Reset()
        {
            IsSolo = false;
            IsMuted = false;
        }
    }
}