using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.PerformerObjects
{
    [CreateAssetMenu(fileName = "New Sequence Performer", menuName = "Anywhen/Performers/Sequence", order = 51)]
    public class PerformerObjectSequence : PerformerObjectBase
    {
        [FormerlySerializedAs("noteSelectStyle")] [Header("SEQUENCE SETTINGS")]
        public SequenceProgressionStyles noteSequenceProgressionStyle;

        public int[] noteSequence;
        //private int _step;

        public override NoteEvent MakeNote(int sequenceStep, AnywhenInstrument instrument)
        {
            if (instrument == null)
            {
                Debug.Log("No instrument selected for puzzle");
                return default;
            }

            int note = noteSequence[GetSequenceStep(noteSequenceProgressionStyle, noteSequence.Length)];
            noteOnEvent = new NoteEvent(note, NoteEvent.EventTypes.NoteOn, GetVolume(), GetTiming());
            return noteOnEvent;
        }


#if UNITY_EDITOR
        public void EditorAdjustNotes(int amount)
        {
            for (int i = 0; i < noteSequence.Length; i++)
            {
                noteSequence[i] += amount;
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}