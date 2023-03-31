using System;
using PackageAnywhen.Runtime.Anywhen;
using Rytmos.AudioSystem.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Rytmos.AudioSystem
{
    [CreateAssetMenu(fileName = "New AudioPlayer", menuName = "Anywhen/Performers/AudioPlayer - Sequence", order = 51)]
    public class PerformerObjectSequence : PerformerObjectBase
    {
        [Header("SEQUENCE SETTINGS")] public SelectStyles noteSelectStyle;

        public int[] noteSequence;
        //private int _step;

        public override void Play(int sequenceStep, AnywhenInstrument instrument)
        {
            if (instrument == null)
            {
                Debug.Log("No instrument selected for puzzle");
                return;
            }

            int note = 0;
            switch (noteSelectStyle)
            {
                case SelectStyles.Sequence:
                    note = noteSequence.Length == 0
                        ? 0
                        : noteSequence[(int)Mathf.Repeat(sequenceStep, noteSequence.Length)];


                    break;
                case SelectStyles.Random:
                    note = noteSequence.Length == 0 ? 0 : noteSequence[Random.Range(0, noteSequence.Length)];

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


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