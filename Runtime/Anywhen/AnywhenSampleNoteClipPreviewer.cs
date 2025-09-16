using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenSampleNoteClipPreviewer : AnywhenSampleVoice
    {
        public void PlayClip(AnywhenSampleInstrument instrument)
        {
            //Init();
            SetInstrument(instrument);
            NoteOn(0, 0, -1, 1);
        }

        public void StopClip()
        {
            StopScheduled(1);
        }

    }
}