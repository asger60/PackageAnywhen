using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen
{
    public static class AnywhenSnapshotBlender
    {
        public static void ApplyBlend(AnysongObject song, float mixValue, List<AnywhenPlayerBase.PlayerTrack> playerTracks = null)
        {
            if (!song || song.snapshotA == null || song.snapshotB == null) return;

            var snapshotA = song.snapshotA.Snapshot;
            var snapshotB = song.snapshotB.Snapshot;

            if (snapshotA == null || snapshotB == null || snapshotA.Count == 0 || snapshotB.Count == 0) return;

            var bLookup = new Dictionary<string, AnywhenSnapshot.PropertyValue>(snapshotB.Count);
            foreach (var pv in snapshotB) bLookup[pv.path] = pv;

            foreach (var a in snapshotA)
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

                // Track properties: Tracks.Array.data[0].volume
                if (a.path.StartsWith("Tracks.Array.data["))
                {
                    var openBracketIdx = a.path.IndexOf('[');
                    var closeBracketIdx = a.path.IndexOf(']');
                    if (openBracketIdx == -1 || closeBracketIdx == -1) continue;

                    string indexStr = a.path.Substring(openBracketIdx + 1, closeBracketIdx - openBracketIdx - 1);
                    if (!int.TryParse(indexStr, out int trackIndex)) continue;

                    if (trackIndex < 0 || trackIndex >= song.Tracks.Count) continue;
                    var track = song.Tracks[trackIndex];

                    string propertyPath = a.path.Substring(closeBracketIdx + 2); // skip "]."

                    // Filter properties: trackFilters.Array.data[0].lowPassSettings.cutoffFrequency
                    if (propertyPath.StartsWith("trackFilters.Array.data["))
                    {
                        var fOpenBracketIdx = propertyPath.IndexOf('[');
                        var fCloseBracketIdx = propertyPath.IndexOf(']');
                        if (fOpenBracketIdx == -1 || fCloseBracketIdx == -1) continue;

                        string fIndexStr = propertyPath.Substring(fOpenBracketIdx + 1, fCloseBracketIdx - fOpenBracketIdx - 1);
                        if (!int.TryParse(fIndexStr, out int filterIndex)) continue;

                        var filters = track.TrackFilters;
                        if (filterIndex < 0 || filterIndex >= filters.Length) continue;

                        var filterSettings = filters[filterIndex];
                        string filterPropertyPath = propertyPath.Substring(fCloseBracketIdx + 2); // skip "]."

                        ApplyLerpToFilter(filterSettings, filterPropertyPath, a, b, mixValue);

                        // Update the runtime filter if it exists
                        if (playerTracks != null && trackIndex < playerTracks.Count)
                        {
                            var playerTrack = playerTracks[trackIndex];
                            if (playerTrack.trackFilters != null && filterIndex < playerTrack.trackFilters.Count)
                            {
                                playerTrack.trackFilters[filterIndex].SetParameters(filterSettings);
                            }
                        }
                    }
                    else
                    {
                        ApplyLerpToTrack(track, propertyPath, a, b, mixValue);

                        // Update the runtime envelope/LFO if it exists
                        if (playerTracks != null && trackIndex < playerTracks.Count)
                        {
                            var playerTrack = playerTracks[trackIndex];
                            if (propertyPath.StartsWith("trackEnvelope."))
                            {
                                playerTrack.trackEnvelope.UpdateSettings(track.trackEnvelope);
                            }
                            else if (propertyPath.StartsWith("trackLFO."))
                            {
                                playerTrack.trackLFO.UpdateSettings(track.trackLFO);
                            }
                            else if (propertyPath == "trackPitch")
                            {
                                playerTrack.trackPitch = track.TrackPitch;
                            }
                        }
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
            else if (path.StartsWith("trackEnvelope."))
            {
                string subPath = path.Substring("trackEnvelope.".Length);
                if (subPath == "attack") trackSettings.trackEnvelope.attack = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "decay") trackSettings.trackEnvelope.decay = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "sustain") trackSettings.trackEnvelope.sustain = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "release") trackSettings.trackEnvelope.release = Mathf.Lerp(a.floatVal, b.floatVal, t);
            }
            else if (path.StartsWith("trackLFO."))
            {
                string subPath = path.Substring("trackLFO.".Length);
                if (subPath == "frequency") trackSettings.trackLFO.frequency = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "amplitude") trackSettings.trackLFO.amplitude = Mathf.Lerp(a.floatVal, b.floatVal, t);
                else if (subPath == "retrigger") trackSettings.trackLFO.retrigger = t >= 0.5f ? b.boolVal : a.boolVal;
            }
        }

        private static void ApplyLerpToFilter(SynthSettingsObjectFilter filter, string path, AnywhenSnapshot.PropertyValue a, AnywhenSnapshot.PropertyValue b, float t)
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
        }
    }
}
