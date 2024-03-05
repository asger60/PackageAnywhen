using System;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen.PerformerObjects
{
    [CreateAssetMenu(fileName = "New ChordPlayer", menuName = "Anywhen/Performers/Chords", order = 51)]
    public class PerformerObjectChords : PerformerObjectBase
    {
        [Serializable]
        public struct Chord
        {
            public int[] notes;
        }

        [Header("CHORD SETTINGS")] public SequenceProgressionStyles chordSequenceProgressionStyle;
        public Chord[] chords;

        [Header("STRUM SETTINGS")] [Range(0, 1f)]
        public float strumDuration = 0;

        [Range(0, 1f)] public float strumHumanize = 0;

        private NoteEvent _currentEvent;

        public override NoteEvent MakeNote(int sequenceStep, AnywhenInstrument instrument)
        {
            if (chords.Length == 0) return default;
            var chord = chords[GetSequenceStep(chordSequenceProgressionStyle, chords.Length)];


            //if (chokeNotes)
            //{
            //    _currentEvent.state = NoteEvent.EventTypes.NoteOff;
            //    EventFunnel.HandleNoteEvent(_currentEvent, instrument);
            //}


            _currentEvent = new NoteEvent
            {
                state = NoteEvent.EventTypes.NoteOn,
                drift = GetTiming(),
                notes = chord.notes,
                chordStrum = CreateStrum(chord),
                velocity = GetVolume()
            };

            return _currentEvent;
        }

        double[] CreateStrum(Chord chord)
        {
            double[] strum = new double[chord.notes.Length];
            double maxStrum = AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub8) * strumDuration;
            float strumRandomLength = (float)maxStrum / (float)chord.notes.Length;
            for (int i = 0; i < strum.Length; i++)
            {
                float sr = Random.Range(-strumRandomLength, strumRandomLength);
                strum[i] = (i / (float)strum.Length) * maxStrum + Mathf.Lerp(0, sr, strumHumanize);
            }

            return strum;
        }
    }
}