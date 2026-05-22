using System.Reflection;
using Anywhen;
using Anywhen.Composing;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;

public class AnywhenUtils : MonoBehaviour
{
    /*
    public static AnysongObject CloneAnysong(AnysongObject source)
    {
        AnysongObject clone = (AnysongObject)ScriptableObject.CreateInstance(source.GetType());
        clone.tempo = source.tempo;
        clone.songVolume = source.songVolume;
        clone.author = source.author;

        clone.Tracks = new System.Collections.Generic.List<AnysongTrackSettings>();
        foreach (var track in source.Tracks)
        {
            clone.Tracks.Add(CloneTrackSettings(track));
        }

        clone.Sections = new System.Collections.Generic.List<AnysongSection>();
        foreach (var section in source.Sections)
        {
            clone.Sections.Add(CloneSection(section));
        }

        // Link cloned tracks in sections to cloned track settings
        for (int s = 0; s < source.Sections.Count; s++)
        {
            for (int t = 0; t < source.Sections[s].tracks.Count; t++)
            {
                if (t < clone.Tracks.Count)
                {
                    clone.Sections[s].tracks[t].anysongTrackSettings = clone.Tracks[t];
                }
            }
        }

        clone.snapshotA = CloneSnapshot(source.snapshotA);
        clone.snapshotB = CloneSnapshot(source.snapshotB);

        return clone;
    }

    private static AnysongTrackSettings CloneTrackSettings(AnysongTrackSettings source)
    {
        var clone = new AnysongTrackSettings
        {
            volume = source.volume,
            instrument = source.instrument,
            trackEnvelope = source.trackEnvelope,
            trackLFO = source.trackLFO,
            TrackPitch = source.TrackPitch,
            intensityMappingCurve = new AnimationCurve(source.intensityMappingCurve.keys),
            voices = source.voices,
            trackType = source.trackType
        };

        if (source.volumeMods != null)
        {
            clone.volumeMods = new ModRouting[source.volumeMods.Length];
            for (int i = 0; i < source.volumeMods.Length; i++)
            {
                clone.volumeMods[i] = CloneModRouting(source.volumeMods[i]);
            }
        }

        if (source.pitchMods != null)
        {
            clone.pitchMods = new ModRouting[source.pitchMods.Length];
            for (int i = 0; i < source.pitchMods.Length; i++)
            {
                clone.pitchMods[i] = CloneModRouting(source.pitchMods[i]);
            }
        }

        if (source.TrackFilters != null)
        {
            var sourceFilters = source.TrackFilters;
            var clonedFilters = new SynthSettingsObjectFilter[sourceFilters.Length];
            for (int i = 0; i < sourceFilters.Length; i++)
            {
                clonedFilters[i] = CloneFilterSettings(sourceFilters[i]);
            }

            // Using reflection to set the private trackFilters field
            typeof(AnysongTrackSettings).GetField("trackFilters", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(clone, clonedFilters);
        }

        return clone;
    }

    private static ModRouting CloneModRouting(ModRouting source)
    {
        if (source == null) return null;
        return new ModRouting
        {
            modSource = source.modSource,
            modAmount = source.modAmount,
            modDepth = new DynamicRangeFloat(source.modDepth.value, source.modDepth.max)
        };
    }

    private static SynthSettingsObjectFilter CloneFilterSettings(SynthSettingsObjectFilter source)
    {
        if (source == null) return null;
        var clone = (SynthSettingsObjectFilter)ScriptableObject.CreateInstance(source.GetType());
        clone.filterType = source.filterType;
        clone.lowPassSettings = source.lowPassSettings;
        clone.ladderSettings = source.ladderSettings;
        clone.bandPassSettings = source.bandPassSettings;
        clone.formantSettings = source.formantSettings;
        clone.bitcrushSettings = source.bitcrushSettings;
        clone.saturatorSettings = source.saturatorSettings;
        clone.delaySettings = source.delaySettings;
        clone.chorusSettings = source.chorusSettings;

        if (source.modRouting != null)
        {
            clone.modRouting = new ModRouting[source.modRouting.Length];
            for (int i = 0; i < source.modRouting.Length; i++)
            {
                clone.modRouting[i] = CloneModRouting(source.modRouting[i]);
            }
        }

        return clone;
    }

    private static AnysongSection CloneSection(AnysongSection source)
    {
        var clone = new AnysongSection
        {
            sectionLength = source.sectionLength,
            progressionSteps = new AnywhenProgressionPatternObject.ProgressionStep[source.progressionSteps.Length]
        };
        for (int i = 0; i < source.progressionSteps.Length; i++)
        {
            clone.progressionSteps[i] = new AnywhenProgressionPatternObject.ProgressionStep
            {
                rootNote = source.progressionSteps[i].rootNote,
                anywhenScale = source.progressionSteps[i].anywhenScale
            };
        }

        clone.tracks = new System.Collections.Generic.List<AnysongSectionTrack>();
        foreach (var track in source.tracks)
        {
            clone.tracks.Add(CloneSectionTrack(track));
        }

        return clone;
    }

    private static AnysongSectionTrack CloneSectionTrack(AnysongSectionTrack source)
    {
        var clone = new AnysongSectionTrack
        {
            patternProgressionType = source.patternProgressionType,
            anysongTrackSettings =
                source.anysongTrackSettings, // This might need to point to the cloned track settings if we want a TRULY complete clone
            patterns = new System.Collections.Generic.List<AnysongPattern>()
        };

        foreach (var pattern in source.patterns)
        {
            clone.patterns.Add(ClonePattern(pattern));
        }

        return clone;
    }

    private static AnysongPattern ClonePattern(AnysongPattern source)
    {
        var clone = new AnysongPattern
        {
            patternLength = source.patternLength,
            steps = new System.Collections.Generic.List<AnysongPatternStep>(),
            triggerChances = new System.Collections.Generic.List<float>(source.triggerChances)
        };

        foreach (var step in source.steps)
        {
            clone.steps.Add(ClonePatternStep(step));
        }

        return clone;
    }

    private static AnysongPatternStep ClonePatternStep(AnysongPatternStep source)
    {
        var clone = new AnysongPatternStep
        {
            noteOn = source.noteOn,
            duration = source.duration,
            offset = source.offset,
            velocity = source.velocity,
            mixWeight = source.mixWeight,
            strumAmount = source.strumAmount,
            strumRandom = source.strumRandom,
            stepRepeats = source.stepRepeats,
            chance = source.chance,
            expression = source.expression,
            rootNote = source.rootNote,
            repeatRate = source.repeatRate,
            chordNotes = new System.Collections.Generic.List<int>(source.chordNotes)
        };
        return clone;
    }

    private static AnywhenSnapshot CloneSnapshot(AnywhenSnapshot source)
    {
        var clone = new AnywhenSnapshot();
        clone.Snapshot.AddRange(source.Snapshot); // PropertyValue is a struct, so this is a value copy
        return clone;
    }
    */
}