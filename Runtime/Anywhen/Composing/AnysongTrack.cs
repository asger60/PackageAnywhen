using System;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongTrack
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        private NoteEvent _lastTrackEvent;
        public AnywhenSampleInstrument.EnvelopeSettings trackEnvelope;

        public AnimationCurve intensityMappingCurve =
            new(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        public enum AnyTrackTypes
        {
            None = 0,
            Bass = 10,
            Pad = 20,
            Lead = 30,
            SynthShort = 35,
            SynthLong = 36,
            [InspectorName("Rhythm/Hihat")] Hihat = 40,
            [InspectorName("Rhythm/Kick")] Kick = 50,
            [InspectorName("Rhythm/Snare")] Snare = 60,
            [InspectorName("Rhythm/Clap")] Clap = 70,
        }

        public AnyTrackTypes trackType;

        public void Init()
        {
            volume = 1;
            intensityMappingCurve = new AnimationCurve(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });
        }

        public AnysongTrack Clone()
        {
            var clone = new AnysongTrack
            {
                instrument = instrument,
                volume = volume,
                intensityMappingCurve = intensityMappingCurve,
                trackEnvelope = trackEnvelope,
                trackType = trackType,
            };
            return clone;
        }

        public void TriggerStep(AnyPatternStep anyPatternStep, AnyPattern pattern, int rootMod)
        {
            _lastTrackEvent = anyPatternStep.GetEvent(pattern.rootNote + rootMod);
            _lastTrackEvent.velocity *= volume;
            AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackEvent, instrument, this);
            foreach (var repeat in anyPatternStep.GetRepeats(pattern.rootNote, volume))
            {
                AnywhenRuntime.EventFunnel.HandleNoteEvent(repeat, instrument, this);
            }
        }

        public void Reset()
        {
        }
    }
}