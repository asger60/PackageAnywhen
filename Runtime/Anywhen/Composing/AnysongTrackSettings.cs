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
        [SerializeField] List<AudioSourceSettings> audioSources;

        public List<AudioSourceSettings> AudioSources
        {
            get
            {
                if (audioSources == null || audioSources.Count == 0)
                {
                    AudioSourceSettings newSettings = new AudioSourceSettings();
                    newSettings.audioSourceType = AudioSourceSettings.AudioSourceTypes.Sample;
                    newSettings.sampleSourceSettings.sampleInstrument = instrument as AnywhenSampleInstrument;
                    newSettings.sampleSourceSettings.sourceVolume = 1;
                    audioSources = new List<AudioSourceSettings> { newSettings };
                }

                return audioSources;
            }
        }

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
            public NativeArray<AudioSourceSettings.Unmanaged> audioSources;
            public float volume;
            public AnywhenSampleInstrument.Unmanaged instrument;

            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope1;
            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope2;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO1;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO2;
            public NativeArray<SynthFilterBase.ModRouting> amplitudeMod;
            public NativeArray<SynthFilterBase.ModRouting> pitchMod;


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

            var sources = new NativeArray<AudioSourceSettings.Unmanaged>(audioSources.Count, Allocator.Persistent);
            for (int i = 0; i < audioSources.Count; i++)
            {
                sources[i] = audioSources[i].ToUnmanaged();
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
                amplitudeMod = new NativeArray<SynthFilterBase.ModRouting>(volumeMods, Allocator.Persistent),
                pitchMod = new NativeArray<SynthFilterBase.ModRouting>(pitchMods, Allocator.Persistent),
                audioSources = new NativeArray<AudioSourceSettings.Unmanaged>(sources, Allocator.Persistent),
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

        public void AddAudioSource(AudioSourceSettings settings)
        {
            AudioSources.Add(settings);
        }

        public void UpgradeToSources()
        {
            if (audioSources == null || audioSources.Count == 0)
            {
                AudioSourceSettings newSettings = new AudioSourceSettings();
                newSettings.audioSourceType = AudioSourceSettings.AudioSourceTypes.Sample;
                newSettings.sampleSourceSettings.sampleInstrument = instrument as AnywhenSampleInstrument;
                newSettings.sampleSourceSettings.sourceVolume = 1;
                audioSources = new List<AudioSourceSettings> { newSettings };
            }
        }
    }
}