using System;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using Unity.Collections;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrackSettings
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        public SynthFilterBase.ModRouting[] volumeMods;

        public AudioProcessorSettingsObject.EnvelopeSettings trackAudioEnvelope1;
        public AudioProcessorSettingsObject.EnvelopeSettings trackAudioEnvelope2;

        public AudioProcessorSettingsObject.LFOSettings trackAudioLFO1;
        public AudioProcessorSettingsObject.LFOSettings trackAudioLFO2;

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
            trackAudioEnvelope1 = new AudioProcessorSettingsObject.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
            trackAudioLFO1 = new AudioProcessorSettingsObject.LFOSettings(2, 0.01f);
        }

        public AnysongTrackSettings Clone()
        {
            var clone = new AnysongTrackSettings
            {
                instrument = instrument,
                volume = volume,
                intensityMappingCurve = intensityMappingCurve,
                trackAudioEnvelope1 = trackAudioEnvelope1,
                trackAudioLFO1 = trackAudioLFO1,
                //trackType = trackType,
                TrackPitch = TrackPitch,
            };
            return clone;
        }

        public struct Unmanaged
        {
            public float volume;
            public AnywhenSampleInstrument.Unmanaged instrument;
            public AudioProcessorSettingsObject.EnvelopeSettings TrackAudioEnvelope1;
            public AudioProcessorSettingsObject.EnvelopeSettings TrackAudioEnvelope2;
            
            public AudioProcessorSettingsObject.LFOSettings TrackAudioLFO1;
            public AudioProcessorSettingsObject.LFOSettings TrackAudioLFO2;
            

            public float trackPitch;
            public int voices;
            //public AnyTrackTypes trackType;
            public int trackTypeIndex;
            public bool isMuted;
            public bool isSolo;
            public NativeArray<AudioProcessorSettingsObject.Unmanaged> trackFilters;
        }

        public Unmanaged ToUnmanaged()
        {
            var filters = new NativeArray<AudioProcessorSettingsObject.Unmanaged>(TrackFilters.Length, Allocator.Persistent);
            for (int i = 0; i < TrackFilters.Length; i++)
            {
                filters[i] = TrackFilters[i].ToUnmanaged();
            }

            return new Unmanaged
            {
                instrument = ((AnywhenSampleInstrument)instrument).ToUnmanaged(),
                volume = volume,
                TrackAudioEnvelope1 = trackAudioEnvelope1,
                TrackAudioEnvelope2 = trackAudioEnvelope2,
                TrackAudioLFO1 = trackAudioLFO1,
                TrackAudioLFO2 = trackAudioLFO2,
                trackPitch = trackPitch,
                voices = voices,
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

        public void CheckForChanges()
        {
            Debug.Log("CheckForChanges");
        }
    }
}