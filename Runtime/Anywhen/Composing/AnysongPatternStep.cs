using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen.Composing
{
    [Serializable]
    public class AnysongPatternStep
    {
        public List<AnysongPatternNote> stepNotes;

        public void ClearNotes()
        {
            stepNotes ??= new List<AnysongPatternNote>();

            stepNotes.Clear();
        }

        public void AddNote(AnysongPatternNote note)
        {
            stepNotes ??= new List<AnysongPatternNote>();
            stepNotes.Add(note);
        }

        public void RemoveNote(AnysongPatternNote note)
        {
            stepNotes ??= new List<AnysongPatternNote>();

            for (var i = 0; i < stepNotes.Count; i++)
            {
                var n = stepNotes[i];
                if (!n.Equals(note)) continue;
                stepNotes.RemoveAt(i);
                return;
            }
        }
        
        public int GetArrayIndex(int noteIndex)
        {
            for (int i = 0; i < stepNotes.Count; i++)
            {
                if (stepNotes[i].noteIndex == noteIndex)
                    return i;
            }
            
            return 0;
        }

        public bool NoteOn => stepNotes?.Count > 0;

        [Range(0.01f, 1f)] public float duration;
        [Range(-1f, 1f)] public float offset;
        [Range(0, 1f)] public float velocity;


        [Range(0, 1f)] public float mixWeight;
        public bool IsChord => chordNotes.Count > 1;


        public List<int> chordNotes;
        [Range(0, 1f)] public float strumAmount;

        [Range(0, 1f)] public float strumRandom;


        [Range(0, 4)] public int stepRepeats;

        [Range(0, 1f)] public float chance;
        [Range(0, 1f)] public float expression;


        public int rootNote;


        public enum RepeatRates
        {
            ThirtyTwo,
            FourtyEight,
            SixtyFour
        }

        public RepeatRates repeatRate;

        public AnysongPatternStep(int i)
        {
            duration = AnywhenAudioMetronome.Sub16Length;
            offset = 0;
            velocity = 1;
            mixWeight = 1;
            chordNotes = new List<int>();
            strumAmount = 0;
            strumRandom = 0;
            stepRepeats = 0;
            chance = 1;
            expression = 0;
            rootNote = 0;
            repeatRate = RepeatRates.ThirtyTwo;
        }


        public struct UnManaged
        {
            public int rootNote;
            public float duration;
            public float offset;
            public float velocity;
            public float mixWeight;
            public NativeArray<int> chordNotes;
            public NativeArray<AnysongPatternNote> StepNotes;
            public float strumAmount;
            public float strumRandom;
            public int stepRepeats;
            public float chance;
            public bool noteOn;
        }

        public UnManaged ToUnmanaged()
        {
            return new UnManaged
            {
                noteOn = chordNotes.Count > 0,
                rootNote = rootNote,
                duration = duration,
                offset = offset,
                velocity = velocity,
                mixWeight = mixWeight,
                chordNotes = new NativeArray<int>(chordNotes.ToArray(), Allocator.Persistent),
                StepNotes = new NativeArray<AnysongPatternNote>(stepNotes.ToArray(), Allocator.Persistent),
                strumAmount = strumAmount,
                strumRandom = strumRandom,
                stepRepeats = stepRepeats,
                chance = chance
            };
        }

        public int[] GetNotes(int patternRoot)
        {
            if (!IsChord)
                return new[] { rootNote + patternRoot };


            var chord = new int[chordNotes.Count + 1];
            chord[0] = rootNote + patternRoot;
            for (int i = 0; i < chordNotes.Count; i++)
            {
                chord[i + 1] = rootNote + chordNotes[i] + patternRoot;
            }

            return chord;
        }


        public void Init()
        {
            duration = (float)AnywhenAudioMetronome.Sub16Length;
            expression = 1;
            velocity = 1;
            chordNotes = new List<int>();
            mixWeight = Random.Range(0, 1f);
        }

        public void Init(int root)
        {
            duration = (float)AnywhenAudioMetronome.Sub16Length;
            expression = 1;
            velocity = 1;
            rootNote = root;
            chordNotes = new List<int>();
            mixWeight = Random.Range(0, 1f);
        }


        NoteEvent GetEvent(int patternRoot)
        {
            NoteEvent.EventTypes type = NoteEvent.EventTypes.NoteOn;


            var e = new NoteEvent(
                GetNotes(patternRoot),
                state: type,
                velocity: velocity,
                drift: offset * AnywhenMetronome.Instance.GetLength(),
                chordStrum: CreateStrum(GetNotes(patternRoot).Length),
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


        double[] CreateStrum(int count)
        {
            if (count == 1)
            {
                return new double[] { 0 };
            }

            var notes = new double[count];
            var maxLength = AnywhenMetronome.Instance.GetLength();
            for (int i = 0; i < notes.Length; i++)

            {
                notes[i] = (maxLength * (strumAmount) * (float)i / (count - 1)) + maxLength * Random.Range(0, strumRandom);
            }

            return notes;
        }

        public AnysongPatternStep Clone()
        {
            var clone = (AnysongPatternStep)MemberwiseClone();
            clone.chordNotes = new List<int>(chordNotes.Count);
            for (var i = 0; i < chordNotes.Count; i++)
            {
                clone.chordNotes[i] = chordNotes[i];
            }


            return clone;
        }

        public bool IsNull()
        {
            return (chordNotes.Count == 0 && duration == 0 && offset == 0 && velocity == 0 && mixWeight == 0);
        }

        public AnysongPatternNote GetNote(int noteIndex)
        {
            stepNotes ??= new List<AnysongPatternNote>();
            
            foreach (var stepNote in stepNotes)
            {
                if (stepNote.noteIndex == noteIndex)
                    return stepNote;
            }

            return new AnysongPatternNote();
        }
    }
}