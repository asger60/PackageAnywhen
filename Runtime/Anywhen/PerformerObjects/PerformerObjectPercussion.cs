using PackageAnywhen.Runtime.Anywhen;
using UnityEngine;
using UnityEngine.Audio;

namespace Rytmos.AudioSystem
{
    [CreateAssetMenu(fileName = "New AudioPlayer", menuName = "Anywhen/Performers/AudioPlayer - Percussion", order = 51)]
    public class PerformerObjectPercussion : PerformerObjectBase
    {
        public override void Play(int sequenceStep, AnywhenInstrument instrument)
        {
            if (instrument == null)
            {
                //Debug.LogWarning("Puzzle " + audioController.transform.name + " is missing instrument");
                return;
            }

            noteOnEvent = new NoteEvent(0, NoteEvent.EventTypes.NoteOn, GetVolume(), playbackRate);
            //noteOnEvent = new Recorder.Event(0, 0, AnywhenMetronome.Instance.GetScheduledPlaytime(playbackRate), GetTiming(),
            //    Vector2.zero, GetVolume(), Recorder.Event.EventTypes.NoteOn);
            
            
            EventFunnel.HandleNoteEvent(noteOnEvent, instrument);
        }
    }
}