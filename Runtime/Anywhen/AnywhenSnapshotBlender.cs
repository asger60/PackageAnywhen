using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.Synth;
using UnityEngine;

namespace Anywhen
{
    public static class AnywhenSnapshotBlender
    {
        public static void ApplyBlend(AnysongObject song, float mixValue)
        {
            if (!song) return;
            //ApplyBlend(song, song.snapshotA, song.snapshotB, mixValue);
            foreach (var track in song.Tracks)
            {
                ApplyBlend(track, track.snapshotA, track.snapshotB, mixValue);
            }
        }

        private static void ApplyBlend(AnysongTrackSettings track, AnywhenSnapshot snapshotA, AnywhenSnapshot snapshotB, float mixValue)
        {
            if (track == null || snapshotA == null || snapshotB == null) return;

            var snapshotAData = snapshotA.Snapshot;
            var snapshotBData = snapshotB.Snapshot;

            if (snapshotAData == null || snapshotBData == null || snapshotAData.Count == 0 || snapshotBData.Count == 0) return;

            var bLookup = new Dictionary<string, AnywhenSnapshot.PropertyValue>(snapshotBData.Count);
            foreach (var pv in snapshotBData) bLookup[pv.path] = pv;

            foreach (var a in snapshotAData)
            {
                if (!bLookup.TryGetValue(a.path, out var b)) continue;

                string propertyPath = a.path;

                // Filter properties: trackFilters[0].lowPassSettings.cutoffFrequency
                if (propertyPath.StartsWith("trackFilters["))
                {
                    var fOpenBracketIdx = propertyPath.IndexOf('[');
                    var fCloseBracketIdx = propertyPath.IndexOf(']');
                    if (fOpenBracketIdx == -1 || fCloseBracketIdx == -1) continue;

                    string fIndexStr = propertyPath.Substring(fOpenBracketIdx + 1, fCloseBracketIdx - fOpenBracketIdx - 1);
                    if (!int.TryParse(fIndexStr, out int filterIndex)) continue;

                    var filters = track.TrackFilters;
                    if (filterIndex < 0 || filterIndex >= filters.Count) continue;

                    var filterSettings = filters[filterIndex];
                    string filterPropertyPath = propertyPath.Substring(fCloseBracketIdx + 2); // skip "]."

                    ApplyLerpToFilter(filterSettings, filterPropertyPath, a, b, mixValue);
                }
                else if (propertyPath.StartsWith("audioSources["))
                {
                    var sOpenBracketIdx = propertyPath.IndexOf('[');
                    var sCloseBracketIdx = propertyPath.IndexOf(']');
                    if (sOpenBracketIdx == -1 || sCloseBracketIdx == -1) continue;

                    string sIndexStr = propertyPath.Substring(sOpenBracketIdx + 1, sCloseBracketIdx - sOpenBracketIdx - 1);
                    if (!int.TryParse(sIndexStr, out int sourceIndex)) continue;

                    var sources = track.AudioSources;
                    if (sourceIndex < 0 || sourceIndex >= sources.Count) continue;

                    var sourceSettings = sources[sourceIndex];
                    string sourcePropertyPath = propertyPath.Substring(sCloseBracketIdx + 2); // skip "]."

                    ApplyLerpToAudioSource(sourceSettings, sourcePropertyPath, a, b, mixValue);
                }
                else
                {
                    ApplyLerpToTrack(track, propertyPath, a, b, mixValue);
                }
            }
        }

        public static void ApplyBlend(AnysongObject song, AnywhenSnapshot snapshotA, AnywhenSnapshot snapshotB, float mixValue)
        {
            if (!song || snapshotA == null || snapshotB == null) return;

            var snapshotAData = snapshotA.Snapshot;
            var snapshotBData = snapshotB.Snapshot;

            if (snapshotAData == null || snapshotBData == null || snapshotAData.Count == 0 || snapshotBData.Count == 0) return;

            var bLookup = new Dictionary<string, AnywhenSnapshot.PropertyValue>(snapshotBData.Count);
            foreach (var pv in snapshotBData) bLookup[pv.path] = pv;

            foreach (var a in snapshotAData)
            {
                if (!bLookup.TryGetValue(a.path, out var b)) continue;

                // Root properties
                if (a.path == "songVolume")
                {
                    song.songVolume = Mathf.Lerp(a.floatVal, b.floatVal, mixValue);
                    continue;
                }

                if (a.path == "tempo")
                {
                    song.tempo = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, mixValue));
                    continue;
                }

                // Track properties: Tracks[0].volume
                if (a.path.StartsWith("Tracks["))
                {
                    var openBracketIdx = a.path.IndexOf('[');
                    var closeBracketIdx = a.path.IndexOf(']');
                    if (openBracketIdx == -1 || closeBracketIdx == -1) continue;

                    string indexStr = a.path.Substring(openBracketIdx + 1, closeBracketIdx - openBracketIdx - 1);
                    if (!int.TryParse(indexStr, out int trackIndex)) continue;

                    if (trackIndex < 0 || trackIndex >= song.Tracks.Count) continue;
                    var track = song.Tracks[trackIndex];

                    string propertyPath = a.path.Substring(closeBracketIdx + 2); // skip "]."

                    // Filter properties: trackFilters[0].lowPassSettings.cutoffFrequency
                    if (propertyPath.StartsWith("trackFilters["))
                    {
                        var fOpenBracketIdx = propertyPath.IndexOf('[');
                        var fCloseBracketIdx = propertyPath.IndexOf(']');
                        if (fOpenBracketIdx == -1 || fCloseBracketIdx == -1) continue;

                        string fIndexStr = propertyPath.Substring(fOpenBracketIdx + 1, fCloseBracketIdx - fOpenBracketIdx - 1);
                        if (!int.TryParse(fIndexStr, out int filterIndex)) continue;

                        var filters = track.TrackFilters;
                        if (filterIndex < 0 || filterIndex >= filters.Count) continue;

                        var filterSettings = filters[filterIndex];
                        string filterPropertyPath = propertyPath.Substring(fCloseBracketIdx + 2); // skip "]."

                        ApplyLerpToFilter(filterSettings, filterPropertyPath, a, b, mixValue);
                    }
                    else if (propertyPath.StartsWith("audioSources["))
                    {
                        var sOpenBracketIdx = propertyPath.IndexOf('[');
                        var sCloseBracketIdx = propertyPath.IndexOf(']');
                        if (sOpenBracketIdx == -1 || sCloseBracketIdx == -1) continue;

                        string sIndexStr = propertyPath.Substring(sOpenBracketIdx + 1, sCloseBracketIdx - sOpenBracketIdx - 1);
                        if (!int.TryParse(sIndexStr, out int sourceIndex)) continue;

                        var sources = track.AudioSources;
                        if (sourceIndex < 0 || sourceIndex >= sources.Count) continue;

                        var sourceSettings = sources[sourceIndex];
                        string sourcePropertyPath = propertyPath.Substring(sCloseBracketIdx + 2); // skip "]."

                        ApplyLerpToAudioSource(sourceSettings, sourcePropertyPath, a, b, mixValue);
                    }
                    else
                    {
                        ApplyLerpToTrack(track, propertyPath, a, b, mixValue);
                    }
                }
            }
        }

        private static void ApplyLerpToTrack(AnysongTrackSettings trackSettings, string path, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
        {
            if (path == "volume") trackSettings.volume = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (path == "trackPitch")
            {
                trackSettings.TrackPitch = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("trackAudioEnvelope1."))
            {
                ApplyLerpToEnvelope(ref trackSettings.trackAudioEnvelope1, path.Substring("trackAudioEnvelope1.".Length), a, b, t);
            }
            else if (path.StartsWith("trackAudioEnvelope2."))
            {
                ApplyLerpToEnvelope(ref trackSettings.trackAudioEnvelope2, path.Substring("trackAudioEnvelope2.".Length), a, b, t);
            }
            else if (path.StartsWith("trackAudioLFO1."))
            {
                ApplyLerpToLFO(ref trackSettings.trackAudioLFO1, path.Substring("trackAudioLFO1.".Length), a, b, t);
            }
            else if (path.StartsWith("trackAudioLFO2."))
            {
                ApplyLerpToLFO(ref trackSettings.trackAudioLFO2, path.Substring("trackAudioLFO2.".Length), a, b, t);
            }
            else if (path == "voices") trackSettings.voices = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
            else if (path == "trackTypeIndex") trackSettings.trackTypeIndex = t >= 0.5f ? b.intVal : a.intVal;
            else if (path == "intensityMappingCurve")
            {
                // AnimationCurves cannot be easily lerped at runtime without custom logic.
                // For now, we just switch at 0.5.
                //trackSettings.intensityMappingCurve = t >= 0.5f ? b.curveVal : a.curveVal;
            }
        }

        private static void ApplyLerpToEnvelope(ref AudioProcessorSettings.EnvelopeSettings settings, string subPath, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
        {
            if (subPath == "attack") settings.attack = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (subPath == "decay") settings.decay = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (subPath == "sustain") settings.sustain = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (subPath == "release") settings.release = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (subPath == "enabled") settings.enabled = t >= 0.5f ? b.boolVal : a.boolVal;
        }

        private static void ApplyLerpToLFO(ref AudioProcessorSettings.LFOSettings settings, string subPath, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
        {
            if (subPath == "frequency") settings.frequency = Mathf.Lerp(a.floatVal, b.floatVal, t);
            else if (subPath == "enabled") settings.enabled = t >= 0.5f ? b.boolVal : a.boolVal;
            else if (subPath == "unipolar") settings.unipolar = t >= 0.5f ? b.boolVal : a.boolVal;
        }

        private static void ApplyLerpToFilter(AudioProcessorSettings filter, string path, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
        {
            if (path.StartsWith("lowPassSettings."))
            {
                string subPath = path.Substring("lowPassSettings.".Length);
                if (subPath == "cutoffFrequency") filter.lowPassSettings.cutoffFrequency = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "resonance") filter.lowPassSettings.resonance = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "oversampling") filter.lowPassSettings.oversampling = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
            }
            else if (path.StartsWith("ladderSettings."))
            {
                string subPath = path.Substring("ladderSettings.".Length);
                if (subPath == "cutoffFrequency") filter.ladderSettings.cutoffFrequency = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "resonance") filter.ladderSettings.resonance = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "oversampling") filter.ladderSettings.oversampling = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
            }
            else if (path.StartsWith("bandPassSettings."))
            {
                string subPath = path.Substring("bandPassSettings.".Length);
                if (subPath == "frequency") filter.bandPassSettings.frequency = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "bandWidth") filter.bandPassSettings.bandWidth = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "q") filter.bandPassSettings.q = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("bitcrushSettings."))
            {
                string subPath = path.Substring("bitcrushSettings.".Length);
                if (subPath == "bitDepth") filter.bitcrushSettings.bitDepth = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "downsampling") filter.bitcrushSettings.downsampling = Mathf.RoundToInt(Mathf.Lerp(a.intVal, b.intVal, t));
            }
            else if (path.StartsWith("saturatorSettings."))
            {
                string subPath = path.Substring("saturatorSettings.".Length);
                if (subPath == "drive") filter.saturatorSettings.drive = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "wet") filter.saturatorSettings.wet = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("delaySettings."))
            {
                string subPath = path.Substring("delaySettings.".Length);
                if (subPath == "delayTime") filter.delaySettings.delayTime = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "feedback") filter.delaySettings.feedback = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "wet") filter.delaySettings.wet = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("reverbSettings."))
            {
                string subPath = path.Substring("reverbSettings.".Length);
                if (subPath == "roomSize") filter.reverbSettings.roomSize = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "damping") filter.reverbSettings.damping = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "wet") filter.reverbSettings.wet = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("chorusSettings."))
            {
                string subPath = path.Substring("chorusSettings.".Length);
                if (subPath == "rate") filter.chorusSettings.rate = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "depth") filter.chorusSettings.depth = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "delay") filter.chorusSettings.delay = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "feedback") filter.chorusSettings.feedback = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "wet") filter.chorusSettings.wet = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("formantSettings."))
            {
                string subPath = path.Substring("formantSettings.".Length);
                if (subPath == "vowel") filter.formantSettings.vowel = t >= 0.5f ? b.intVal : a.intVal;
            }
            else if (path.StartsWith("envelopeSettings."))
            {
                ApplyLerpToEnvelope(ref filter.envelopeSettings, path.Substring("envelopeSettings.".Length), a, b, t);
            }
            else if (path.StartsWith("lfoSettings."))
            {
                ApplyLerpToLFO(ref filter.lfoSettings, path.Substring("lfoSettings.".Length), a, b, t);
            }
        }

        private static void ApplyLerpToAudioSource(AudioSourceSettings source, string path, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
        {
            if (path == "audioSourceType") source.audioSourceType = t >= 0.5f ? (AudioSourceSettings.AudioSourceTypes)b.intVal : (AudioSourceSettings.AudioSourceTypes)a.intVal;
            else if (path.StartsWith("sampleSourceSettings."))
            {
                string subPath = path.Substring("sampleSourceSettings.".Length);
                if (subPath == "sourceVolume") source.sampleSourceSettings.sourceVolume = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("synthSourceSettings."))
            {
                string subPath = path.Substring("synthSourceSettings.".Length);
                if (subPath == "sourceVolume") source.synthSourceSettings.sourceVolume = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "noteOffset") source.synthSourceSettings.noteOffset = t >= 0.5f ? b.intVal : a.intVal;
                else if (subPath == "detune") source.synthSourceSettings.detune = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "synthType") source.synthSourceSettings.synthType = t >= 0.5f ? (AudioSourceSettings.SynthSourceSettings.SynthType)b.intVal : (AudioSourceSettings.SynthSourceSettings.SynthType)a.intVal;
            }
            else if (path.StartsWith("noiseSourceSettings."))
            {
                string subPath = path.Substring("noiseSourceSettings.".Length);
                if (subPath == "sourceVolume") source.noiseSourceSettings.sourceVolume = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "noiseType") source.noiseSourceSettings.noiseType = t >= 0.5f ? (AudioSourceSettings.NoiseSourceSettings.NoiseType)b.intVal : (AudioSourceSettings.NoiseSourceSettings.NoiseType)a.intVal;
            }
        }
    }
}
