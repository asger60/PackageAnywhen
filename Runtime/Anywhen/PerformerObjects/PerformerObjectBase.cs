using System;
using Anywhen.Attributes;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.PerformerObjects
{
    public class PerformerObjectBase : AnywhenSettingsBase
    {
        [Range(0f, 1f)] public float volume = 1;

        protected NoteEvent noteOnEvent;

        public enum SequenceProgressionStyles
        {
            Forward,
            Backwards,
            PingPong,
            Random,
            Drunk
        }

        [MinMaxSlider(-0.5f, 0.5f, Round = true, FlexibleFields = true)]
        public Vector2 volumeRandom;

        public bool[] stepAccents;
        [Range(0,1f)]
        public float stepAccentVolume = 1;
        
        [Header("TIMING")] public AnywhenMetronome.TickRate playbackRate = AnywhenMetronome.TickRate.Sub8;

        [Range(0, 1f)] public float humanizeAmount = 0;

        [Range(0, 1f)] public float swingAmount = 0;
        [Header("CHOKE")] public bool chokeNotes;

        protected double GetTiming()
        {
            //if (humanizeAmount <= 0) return 0;
            float subLength = (float)AnywhenMetronome.Instance.GetLength(playbackRate) / 2f;
            float drift = Random.Range(subLength * -1, subLength);
            float swing = 0;

            if (swingAmount > 0)
            {
                int count = AnywhenMetronome.Instance.GetCountForTickRate(playbackRate);
                if (count % 2 == 0)
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
            bool acc = false;
            if (stepAccents.Length > 0)
            {
                acc = stepAccents[
                    (int)Mathf.Repeat(AnywhenMetronome.Instance.GetCountForTickRate(playbackRate), stepAccents.Length-1)];
            }

            return volume + (acc ? stepAccentVolume : 0) + Random.Range(volumeRandom.x, volumeRandom.y);
        }

        protected int GetSequenceStep(SequenceProgressionStyles currentProgressionStyle, int currentNoteIndex,
            int progressionLenght)
        {
            switch (currentProgressionStyle)
            {
                case SequenceProgressionStyles.Forward:
                    return (int)Mathf.Repeat(currentNoteIndex, progressionLenght);
                case SequenceProgressionStyles.Backwards:
                    var count = (int)Mathf.Repeat((progressionLenght - 1) - currentNoteIndex, progressionLenght);
                    Debug.Log(count);
                    return count;
                case SequenceProgressionStyles.PingPong:
                    return (int)Mathf.PingPong(currentNoteIndex, progressionLenght - 1);
                case SequenceProgressionStyles.Random:
                    return Random.Range(0, progressionLenght);
                case SequenceProgressionStyles.Drunk:
                    return (int)Mathf.Repeat(
                        (int)Mathf.Repeat(currentNoteIndex, progressionLenght) + Random.Range(-2, 1),
                        progressionLenght);
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentProgressionStyle), currentProgressionStyle,
                        null);
            }
        }
    }
}