using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongPatternStep
    {
        public bool noteOn; //depricated value, only here for backwards compatibility

        public bool NoteOn
        {
            get
            {
                if (noteOn)
                {
                    chordNotes.Add(0);
                    noteOn = false;
                    return true;
                }


                return chordNotes.Count > 0;
            }
        }

        [Range(0.01f, 1f)] public float duration;
        [Range(-1f, 1f)] public float offset;
        [Range(0, 1f)] public float velocity;


        [Range(0, 1f)] public float mixWeight;
        public bool IsChord => chordNotes.Count > 1;


        public List<int> chordNotes = new List<int>();
        [Range(0, 1f)] public float strumAmount;

        [Range(0, 1f)] public float strumRandom;


        [Range(0, 4)] public int stepRepeats;

        [Range(0, 1f)] public float chance = 1;
        [Range(0, 1f)] public float expression = 0;

        public int rootNote;

        public enum RepeatRates
        {
            ThirtyTwo,
            FourtyEight,
            SixtyFour
        }

        public RepeatRates repeatRate;

        private int[] _notesCache;
        private int _lastPatternRoot = int.MinValue;
        private int _lastRootNote = int.MinValue;
        private List<int> _lastChordNotes;

        public int[] GetNotes(int patternRoot)
        {
            if (!IsChord)
            {
                int note = rootNote + patternRoot;
                if (_notesCache == null || _notesCache.Length != 1 || _notesCache[0] != note)
                {
                    _notesCache = new[] { note };
                }

                return _notesCache;
            }

            bool chordChanged = _lastChordNotes == null || !chordNotes.SequenceEqual(_lastChordNotes);

            if (_notesCache == null || _notesCache.Length != chordNotes.Count + 1 ||
                _lastPatternRoot != patternRoot || _lastRootNote != rootNote || chordChanged)
            {
                _notesCache = new int[chordNotes.Count + 1];
                _notesCache[0] = rootNote + patternRoot;
                for (int i = 0; i < chordNotes.Count; i++)
                {
                    _notesCache[i + 1] = rootNote + chordNotes[i] + patternRoot;
                }

                _lastPatternRoot = patternRoot;
                _lastRootNote = rootNote;
                _lastChordNotes = new List<int>(chordNotes);
            }

            return _notesCache;
        }


        public void Init()
        {
            duration = 1;
            expression = 1;
            velocity = 1;
            chordNotes = new List<int> { };
            mixWeight = Random.Range(0, 1f);
        }


        NoteEvent GetEvent(int patternRoot)
        {
            NoteEvent.EventTypes type = NoteEvent.EventTypes.NoteOn;

            var notes = GetNotes(patternRoot);
            var strum = CreateStrum(notes.Length);

            var e = new NoteEvent(
                notes,
                state: type,
                velocity: velocity,
                drift: offset * AnywhenMetronome.Instance.GetLength(),
                chordStrum: strum,
                duration: duration,
                expression1: expression,
                expression2: 1
            );


            return e;
        }



        public NoteEvent[] GetNoteEvents(int patternRoot)
        {
            NoteEvent[] events = new NoteEvent[stepRepeats + 1];
            events[0] = GetEvent(patternRoot);
            if (stepRepeats == 0) return events;
            double subDivisionDuration = AnywhenMetronome.Instance.GetLength() / ((int)repeatRate + 2);

            for (int i = 1; i <= stepRepeats; i++)
            {
                events[i] = GetEvent(patternRoot);
                events[i].drift += subDivisionDuration * i;
            }

            return events;
        }
        //public NoteEvent[] GetNoteEvents(int patternRoot)
        //{
        //    
//
        //    var maxLength = AnywhenMetronome.Instance.GetLength();
        //    bool paramsChanged = _lastPatternRoot != patternRoot ||
        //                         !Mathf.Approximately(_lastDuration, duration) ||
        //                         !Mathf.Approximately(_lastOffset, offset) ||
        //                         !Mathf.Approximately(_lastVelocity, velocity) ||
        //                         !Mathf.Approximately(_lastExpression, expression) ||
        //                         _lastRepeatRate != repeatRate ||
        //                         Math.Abs(_lastMetronomeLength - maxLength) > 0.1f;
//
        //    if (_eventsCache == null || _eventsCache.Length != stepRepeats + 1 || paramsChanged)
        //    {
        //        if (_eventsCache == null || _eventsCache.Length != stepRepeats + 1)
        //        {
        //            _eventsCache = new NoteEvent[stepRepeats + 1];
        //        }
//
        //        _eventsCache[0] = GetEvent(patternRoot);
        //        if (stepRepeats > 0)
        //        {
        //            double subDivisionDuration = maxLength / ((int)repeatRate + 2);
//
        //            for (int i = 1; i <= stepRepeats; i++)
        //            {
        //                _eventsCache[i] = GetEvent(patternRoot);
        //                _eventsCache[i].drift += subDivisionDuration * i;
        //            }
        //        }
//
        //        _lastDuration = duration;
        //        _lastOffset = offset;
        //        _lastVelocity = velocity;
        //        _lastExpression = expression;
        //        _lastRepeatRate = repeatRate;
//
        //        _lastPatternRoot = patternRoot;
        //        _lastMetronomeLength = maxLength;
        //    }
//
        //    return _eventsCache;
        //}

        private double[] _strumCache;
        private double _lastMetronomeLength = double.MinValue;
        private float _lastStrumAmount = float.MinValue;

        double[] CreateStrum(int count)
        {
            if (count == 1)
            {
                if (_strumCache == null || _strumCache.Length != 1 || _strumCache[0] != 0)
                {
                    _strumCache = new double[] { 0 };
                }

                return _strumCache;
            }

            var maxLength = AnywhenMetronome.Instance.GetLength();
            if (_strumCache == null || _strumCache.Length != count || strumRandom > 0 ||
                Math.Abs(_lastMetronomeLength - maxLength) > 0.1f || !Mathf.Approximately(_lastStrumAmount, strumAmount))
            {
                _strumCache = new double[count];
                for (int i = 0; i < _strumCache.Length; i++)
                {
                    _strumCache[i] = (maxLength * (strumAmount) * (float)i / (count - 1)) +
                                     maxLength * Random.Range(0, strumRandom);
                }

                _lastMetronomeLength = maxLength;
                _lastStrumAmount = strumAmount;
            }

            return _strumCache;
        }

        public AnysongPatternStep Clone()
        {
            var clone = (AnysongPatternStep)MemberwiseClone();
            clone.chordNotes = new List<int>();
            foreach (var note in chordNotes)
            {
                clone.chordNotes.Add(note);
            }

            clone._notesCache = null;
            clone._strumCache = null;
            clone._lastChordNotes = null;

            return clone;
        }
    }
}