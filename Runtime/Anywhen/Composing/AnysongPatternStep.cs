using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


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


        public struct UnManaged
        {
            public NativeArray<AnysongPatternNote> StepNotes;
        }

        public UnManaged ToUnmanaged()
        {
            return new UnManaged
            {
                StepNotes = new NativeArray<AnysongPatternNote>(stepNotes.ToArray(), Allocator.Persistent),
            };
        }


        public void Init()
        {
            stepNotes = new List<AnysongPatternNote>();
        }


        public AnysongPatternStep Clone()
        {
            var clone = (AnysongPatternStep)MemberwiseClone();
            stepNotes ??= new List<AnysongPatternNote>();
            clone.stepNotes = new List<AnysongPatternNote>(stepNotes.Count);
            Debug.Log("creating copy " + stepNotes.Count);
            for (var i = 0; i < stepNotes.Count; i++)
            {
                clone.stepNotes.Add(stepNotes[i]);
            }


            return clone;
        }

        public bool IsNull()
        {
            return (stepNotes.Count == 0);
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

        public void ShiftDown()
        {
            throw new NotImplementedException();
        }

        public void ShiftUp()
        {
            throw new NotImplementedException();
        }
    }
}