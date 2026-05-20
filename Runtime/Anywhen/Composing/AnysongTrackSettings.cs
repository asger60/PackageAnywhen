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
                    //newSettings.sampleSourceSettings.sampleInstrument = instrument as AnywhenSampleInstrument;
                    newSettings.sampleSourceSettings.sourceVolume = 1;
                    audioSources = new List<AudioSourceSettings> { newSettings };
                }

                return audioSources;
            }
        }

        public bool IsPercussionTrack()
        {
            if (AudioSources.Count > 0)
            {
                var sampleInstrument = AudioSources[0].sampleSourceSettings.sampleInstrument;
                if (sampleInstrument && sampleInstrument.clipSelectType == AnywhenSampleInstrument.ClipSelectType.Percussion)
                    return true;
            }

            return false;
        }

        [Range(0, 1f)] public float volume;

        //public AnywhenInstrument instrument;
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


        [Range(1, 16)] public int voices = 1;
        [AnywhenTrackType] public int trackTypeIndex;

        public AnywhenSnapshot snapshotA = new();
        public AnywhenSnapshot snapshotB = new();

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
            trackAudioEnvelope1 = new AudioProcessorSettings.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f, true);
            trackAudioEnvelope2 = new AudioProcessorSettings.EnvelopeSettings(0.01f, 0.5f, 1, 0.1f, false);
            trackAudioLFO1 = new AudioProcessorSettings.LFOSettings(2, 0.01f, false);
            trackAudioLFO2 = new AudioProcessorSettings.LFOSettings(2, 0.01f, false);
        }

        public AnysongTrackSettings Clone()
        {
            var clone = new AnysongTrackSettings
            {
                //instrument = instrument,
                volume = volume,
                trackAudioEnvelope1 = trackAudioEnvelope1,
                trackAudioEnvelope2 = trackAudioEnvelope2,
                trackAudioLFO1 = trackAudioLFO1,
                trackAudioLFO2 = trackAudioLFO2,
                TrackPitch = TrackPitch,
                voices = voices,
                trackTypeIndex = trackTypeIndex,
                snapshotA = snapshotA.Clone(),
                snapshotB = snapshotB.Clone(),
                volumeMods = volumeMods != null ? (SynthFilterBase.ModRouting[])volumeMods.Clone() : null,
                pitchMods = pitchMods != null ? (SynthFilterBase.ModRouting[])pitchMods.Clone() : null,
                trackFilters = new List<AudioProcessorSettings>(TrackFilters),
                audioSources = new List<AudioSourceSettings>(AudioSources)
            };
            return clone;
        }

        public struct Unmanaged
        {
            public NativeArray<AudioSourceSettings.Unmanaged> audioSources;
            public float Volume;
            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope1;
            public AudioProcessorSettings.EnvelopeSettings TrackAudioEnvelope2;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO1;
            public AudioProcessorSettings.LFOSettings TrackAudioLFO2;
            public NativeArray<SynthFilterBase.ModRouting> AmplitudeMod;
            public NativeArray<SynthFilterBase.ModRouting> PitchMod;


            public float trackPitch;

            public int voices;

            public int trackTypeIndex;

            public NativeArray<AudioProcessorSettings.Unmanaged> trackFilters;

            public void Dispose()
            {
                if (audioSources.IsCreated) audioSources.Dispose();
                if (AmplitudeMod.IsCreated) AmplitudeMod.Dispose();
                if (PitchMod.IsCreated) PitchMod.Dispose();
                if (trackFilters.IsCreated)
                {
                    for (int i = 0; i < trackFilters.Length; i++)
                    {
                        trackFilters[i].Dispose();
                    }

                    trackFilters.Dispose();
                }
            }

            public bool Equals(Unmanaged other)
            {
                return
                    Mathf.Approximately(Volume, other.Volume) &&
                    trackTypeIndex == other.trackTypeIndex &&
                    trackFilters.Length == other.trackFilters.Length &&
                    audioSources.Length == other.audioSources.Length &&
                    AmplitudeMod.Equals(other.AmplitudeMod) &&
                    PitchMod.Equals(other.PitchMod);
            }
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
                Volume = volume,
                TrackAudioEnvelope1 = trackAudioEnvelope1,
                TrackAudioEnvelope2 = trackAudioEnvelope2,
                TrackAudioLFO1 = trackAudioLFO1,
                TrackAudioLFO2 = trackAudioLFO2,
                trackPitch = trackPitch,
                voices = voices,
                trackTypeIndex = trackTypeIndex,
                trackFilters = filters,
                AmplitudeMod = new NativeArray<SynthFilterBase.ModRouting>(volumeMods, Allocator.Persistent),
                PitchMod = new NativeArray<SynthFilterBase.ModRouting>(pitchMods, Allocator.Persistent),
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
                //newSettings.sampleSourceSettings.sampleInstrument = instrument as AnywhenSampleInstrument;
                newSettings.sampleSourceSettings.sourceVolume = 1;
                audioSources = new List<AudioSourceSettings> { newSettings };
            }
        }
    }
}