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

        [Header("CHORD SETTINGS")] public SelectStyles chordSelectStyle;
        public Chord[] chords;

        [FormerlySerializedAs("strumAmount")] [Header("STRUM SETTINGS")] [Range(0, 1f)]
        public float strumDuration = 0;

        [FormerlySerializedAs("strumRandom")] [Range(0, 1f)]
        public float strumHumanize = 0;

        private NoteEvent _currentEvent;
//        private Recorder.Event _currentEvent;

        public override void Play(int sequenceStep, AnywhenInstrument instrument)
        {
            Chord chord;
            if (chords.Length == 0) return;
            switch (chordSelectStyle)
            {
                case SelectStyles.SequenceForward:
                    chord = chords[(int)Mathf.Repeat(sequenceStep, chords.Length)];
                    break;
                case SelectStyles.Random:
                    chord = chords[Random.Range(0, chords.Length)];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            if (chokeNotes)
            {
                _currentEvent.state = NoteEvent.EventTypes.NoteOff;
                EventFunnel.HandleNoteEvent(_currentEvent, instrument);
            }

            //var step = Metronome.Instance.GetQuantizedStep();
            //_currentEvent = new Recorder.Event(0, chord.notes,
            //    AnywhenMetronome.Instance.GetScheduledPlaytime(playbackRate), GetTiming(), CreateStrum(chord),
            //    Vector2.zero, GetVolume(), Recorder.Event.EventTypes.NoteOn);

            _currentEvent = new NoteEvent(0, 0, playbackRate, chord.notes, CreateStrum(chord), 0, 0, GetVolume());
            
            
            EventFunnel.HandleNoteEvent(_currentEvent, instrument);
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