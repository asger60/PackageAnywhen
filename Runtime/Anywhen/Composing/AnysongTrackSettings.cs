using System;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrackSettings
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        public SynthFilterBase.ModRouting[] volumeMods;

        [FormerlySerializedAs("trackEnvelope")] public AudioEnvelopeSettings trackAudioEnvelope;
        [FormerlySerializedAs("trackLFO")] public AudioLFOSettings trackAudioLFO;

        [Range(0, 10)] [SerializeField] float trackPitch = 1;

        public float TrackPitch
        {
            get => trackPitch;
            set => trackPitch = value;
        }

        public SynthFilterBase.ModRouting[] pitchMods;

        public AnimationCurve intensityMappingCurve = new(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        //public bool monophonic;
        [Range(1, 16)] public int voices = 1;

        [NonSerialized] public bool IsMuted = false;
        [NonSerialized] public bool IsSolo = false;

        public enum AnyTrackTypes
        {
            None = 0,
            Bass = 10,
            Rhythm = 20,
            Notes = 30,
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
        [AnywhenTrackType] public int trackTypeIndex;

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
            trackAudioEnvelope = new AudioEnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
            trackAudioLFO = new AudioLFOSettings(2, 0.01f);
        }

        public AnysongTrackSettings Clone()
        {
            var clone = new AnysongTrackSettings
            {
                instrument = instrument,
                volume = volume,
                intensityMappingCurve = intensityMappingCurve,
                trackAudioEnvelope = trackAudioEnvelope,
                trackAudioLFO = trackAudioLFO,
                trackType = trackType,
                TrackPitch = TrackPitch,
            };
            return clone;
        }

        public struct Unmanaged
        {
            public float volume;
            public AnywhenSampleInstrument.Unmanaged instrument;
            public AudioEnvelopeSettings TrackAudioEnvelope;
            public AudioLFOSettings TrackAudioLFO;
            
            public float trackPitch;
            public int voices;
            public AnyTrackTypes trackType;
            public int trackTypeIndex;
            public bool isMuted;
            public bool isSolo;

            // Note: AnimationCurve and ModRouting[] are not included as they are managed/non-blittable.
            // If they are needed in Burst, they must be passed via NativeArrays or sampled beforehand.
        }

        public Unmanaged ToUnmanaged()
        {
            return new Unmanaged
            {
                instrument = ((AnywhenSampleInstrument)instrument).ToUnmanaged(),
                volume = volume,
                TrackAudioEnvelope = trackAudioEnvelope,
                TrackAudioLFO = trackAudioLFO,
                trackPitch = trackPitch,
                voices = voices,
                trackType = trackType,
                trackTypeIndex = trackTypeIndex,
                isMuted = IsMuted,
                isSolo = IsSolo
            };
            
        }


        public void Reset()
        {
        }

        public void UnMute()
        {
            IsSolo = false;
            IsMuted = false;

        }
    }
}