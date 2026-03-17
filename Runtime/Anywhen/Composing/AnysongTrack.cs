using System;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrack
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        public SynthFilterBase.ModRouting[] volumeMods;

        public AnywhenSampleInstrument.EnvelopeSettings trackEnvelope;

        [FormerlySerializedAs("pitchLFOSettings")]
        public AnywhenSampleInstrument.PitchLFOSettings trackLFO;

        [Range(0, 10)] [SerializeField] float trackPitch = 1;
        public float TrackPitch
        {
            get => trackPitch;
            set => trackPitch = value;
        }
        public SynthFilterBase.ModRouting[] pitchMods;

        public AnimationCurve intensityMappingCurve = new(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

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

        [SerializeField] private SynthSettingsObjectFilter[] trackFilters;

        public SynthSettingsObjectFilter[] TrackFilters
        {
            get
            {
                trackFilters ??= Array.Empty<SynthSettingsObjectFilter>();
                return trackFilters;
            }
        }

        public void Init()
        {
            volume = 1;
            intensityMappingCurve = new AnimationCurve(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });
            trackEnvelope = new AnywhenSampleInstrument.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
            trackLFO = new AnywhenSampleInstrument.PitchLFOSettings(2, 0.01f, false);
        }

        public AnysongTrack Clone()
        {
            var clone = new AnysongTrack
            {
                instrument = instrument,
                volume = volume,
                intensityMappingCurve = intensityMappingCurve,
                trackEnvelope = trackEnvelope,
                trackLFO = trackLFO,
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