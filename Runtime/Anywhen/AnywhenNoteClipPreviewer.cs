using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenNoteClipPreviewer : AnywhenVoice
    {
        public void PlayClip(AnywhenSampleInstrument instrument)
        {
            //Init();
            SetInstrument(instrument);
            NoteOn(0, 0, -1, 1, instrument,new AnywhenSampleInstrument.EnvelopeSettings(0,0,1,0));
        }

        public void StopClip()
        {
            StopScheduled(1);
        }

    }
}