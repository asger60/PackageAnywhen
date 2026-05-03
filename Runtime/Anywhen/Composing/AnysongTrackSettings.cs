using System;
using System.Collections.Generic;
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

        public AudioProcessorSettings.EnvelopeSettings trackAudioEnvelope1;
        public AudioProcessorSettings.EnvelopeSettings trackAudioEnvelope2;

        public AudioProcessorSettings.LFOSettings trackAudioLFO1;
        public AudioProcessorSettings.LFOSettings trackAudioLFO2;

        [Range(0, 10)] [SerializeField] float trackPitch = 1;

        public float TrackPitch
        {
            get => trackPitch;
            set => trackPitch = value;
        }

        public SynthFilterBase.ModRouting[] pitchMods;

        public enum AudioSourceType
        {
            Sample,
            Synth
        }

        public AudioSourceType audioSourceType;

        public enum SynthOscillatorTypes
        {
            Sine,
            Saw,
            Square,
        }

        public SynthOscillatorTypes synthOscillatorType;
        [Range(1, 16)] public int voices = 1;
        [AnywhenTrackType] public int trackTypeIndex;
        [SerializeField] private List<AudioProcessorSettings> trackFilters;


        public List<AudioProcessorSettings> TrackFilters
        {
            get
            {
                trackFilters ??= new List<AudioProcessorSettings>();
                return trackFilters;
            }
        }

        public void Init()
        {
            volume = 1;
            trackAudioEnvelope1 = new AudioProcessorSettings.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f);
            trackAudioLFO1 = new AudioProcessorSettings.LFOSettings(2, 0.01f);
        }

        public AnysongTrackSettings Clone()
        {
            var clone = new AnysongTrackSettings
            {
                instrument = instrument,
                volume = volume,
                trackAudioEnvelope1 = trackAudioEnvelope1,
                trackAudioLFO1 = trackAudioLFO1,
                TrackPitch = TrackPitch,
            };
            return clone;
        }

        public struct Unmanaged
        {
            public float volume;
            public AnywhenSampleInstrument.Unmanaged instrument;

            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope1;
            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope2;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO1;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO2;
            public AudioSourceType audioSourceType;
            public NativeArray<SynthFilterBase.ModRouting> amplitudeMod;
            public NativeArray<SynthFilterBase.ModRouting> pitchMod;


            public SynthOscillatorTypes synthOscillatorType;

            public float trackPitch;

            public int voices;

            //public AnyTrackTypes trackType;
            public int trackTypeIndex;

            public NativeArray<AudioProcessorSettings.Unmanaged> trackFilters;
        }

        public Unmanaged ToUnmanaged()
        {
            var filters = new NativeArray<AudioProcessorSettings.Unmanaged>(TrackFilters.Count, Allocator.Persistent);
            for (int i = 0; i < TrackFilters.Count; i++)
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
                trackFilters = filters,
                audioSourceType = audioSourceType,
                synthOscillatorType = synthOscillatorType,
                amplitudeMod = new NativeArray<SynthFilterBase.ModRouting>(volumeMods, Allocator.Persistent),
                pitchMod = new NativeArray<SynthFilterBase.ModRouting>(pitchMods, Allocator.Persistent)
            };
        }


        public void Reset()
        {
        }


        public void RemoveAudioProcessor(AudioProcessorSettings settings)
        {
            foreach (var filter in TrackFilters)
            {
                if (filter == settings)
                {
                    trackFilters.Remove(filter);
                    break;
                }
            }
        }

        public void AddAudioProcessor(AudioProcessorSettings settings)
        {
            TrackFilters.Add(settings);
        }
    }
}