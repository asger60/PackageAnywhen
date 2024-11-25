using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenNoteClipPreviewer : AnywhenSampler
    {
        public void PlayClip(AnywhenSampleInstrument instrument, AnywhenNoteClip clip)
        {
            Init();
            SetInstrument(instrument);
            //Volume = instrument.volume;
            NoteOn(0, 0, -1, 1, instrument,new AnywhenSampleInstrument.EnvelopeSettings(0,0,1,0));
            //PlayScheduled(clip);
        }

        public void StopClip()
        {
            StopScheduled(1);
        }

    }
}