using Anywhen.Attributes;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen.PerformerObjects
{
    public class PerformerObjectBase : AnywhenSettingsBase
    {
        [Range(0f, 1f)] public float volume = 1;

        protected NoteEvent noteOnEvent;

        public enum SelectStyles
        {
            SequenceForward,
            SequenceBackwards,
            SequencePingPong,
            Random
        }

        [MinMaxSlider(-0.5f, 0.5f, Round = true, FlexibleFields = true)]
        public Vector2 volumeRandom;

        public int[] stepAccents;

        [Header("TIMING")] public AnywhenMetronome.TickRate playbackRate = AnywhenMetronome.TickRate.Sub8;

        [Range(0, 1f)] public float humanizeAmount = 0;

        [Range(0, 1f)] public float swingAmount = 0;
        [Header("CHOKE")] public bool chokeNotes;

        protected double GetTiming()
        {
            if (humanizeAmount >= 1) return 0;
            float subLength = (float)AnywhenMetronome.Instance.GetLength(playbackRate) / 2f;
            float drift = Random.Range(subLength * -1, subLength);
            float swing = 0;

            if (swingAmount > 0)
            {
                if (AnywhenMetronome.Instance.GetCountForTickRate(playbackRate) % 2 != 0)
                    swing = subLength * swingAmount;
            }

            return Mathf.Lerp(0, drift, humanizeAmount) + swing;
        }


        public virtual void Play(int sequenceStep, AnywhenInstrument instrument)
        {
            noteOnEvent = new NoteEvent(0, NoteEvent.EventTypes.NoteOn, GetVolume(), playbackRate, GetTiming());
            EventFunnel.HandleNoteEvent(noteOnEvent, instrument);
        }


        protected virtual float GetVolume()
        {
            return volume + Random.Range(volumeRandom.x, volumeRandom.y);
        }

        protected int GetSequenceStep(int currentNoteIndex)
        {
            //switch (se)
            //{
            //    
            //}
            return 0;
        }
    }
}