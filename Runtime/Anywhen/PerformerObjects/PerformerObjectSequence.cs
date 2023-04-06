#if UNITY_EDITOR
#endif
using System;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen.PerformerObjects
{
    [CreateAssetMenu(fileName = "New AudioPlayer", menuName = "Anywhen/Performers/AudioPlayer - Sequence", order = 51)]
    public class PerformerObjectSequence : PerformerObjectBase
    {
        [FormerlySerializedAs("noteSelectStyle")] [Header("SEQUENCE SETTINGS")]
        public SequenceProgressionStyles noteSequenceProgressionStyle;

        public int[] noteSequence;
        //private int _step;

        public override void Play(int sequenceStep, AnywhenInstrument instrument)
        {
            if (instrument == null)
            {
                Debug.Log("No instrument selected for puzzle");
                return;
            }

            int note = noteSequence[ GetSequenceStep(noteSequenceProgressionStyle, sequenceStep, noteSequence.Length)];


            noteOnEvent = new NoteEvent(note, NoteEvent.EventTypes.NoteOn, GetVolume(), playbackRate, GetTiming());

            EventFunnel.HandleNoteEvent(noteOnEvent, instrument);
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