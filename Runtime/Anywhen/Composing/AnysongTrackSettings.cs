using System;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
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

        [FormerlySerializedAs("trackEnvelope")] public AudioProcessorSettingsObject.EnvelopeSettings trackAudioEnvelope;
        [FormerlySerializedAs("trackLFO")] public AudioProcessorSettingsObject.LFOSettings trackAudioLFO;

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

        [SerializeField] private AudioProcessorSettingsObject[] trackFilters;

        public AudioProcessorSettingsObject[] TrackFilters
        {
            get
            {
                trackFilters ??= Array.Empty<AudioProcessorSettingsObject>();
                return trackFilters;
            }
        }

        public void Init()
        {
            volume = 1;
            intensityMappingCurve = new AnimationCurve(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });
            trackAudioEnvelope = new AudioProcessorSettingsObject.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
            trackAudioLFO = new AudioProcessorSettingsObject.LFOSettings(2, 0.01f);
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
            public AudioProcessorSettingsObject.EnvelopeSettings TrackAudioEnvelope;
            public AudioProcessorSettingsObject.LFOSettings TrackAudioLFO;
            
            public float trackPitch;
            public int voices;
            public AnyTrackTypes trackType;
            public int trackTypeIndex;
            public bool isMuted;
            public bool isSolo;
            public NativeArray<AudioProcessorSettingsObject.Unmanaged> trackFilters;
        }

        public Unmanaged ToUnmanaged(Allocator allocator = Allocator.Temp)
        {
            var filters = new NativeArray<AudioProcessorSettingsObject.Unmanaged>(TrackFilters.Length, allocator);
            for (int i = 0; i < TrackFilters.Length; i++)
            {
                filters[i] = TrackFilters[i].ToUnmanaged();
            }

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
                isSolo = IsSolo,
                trackFilters = filters
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