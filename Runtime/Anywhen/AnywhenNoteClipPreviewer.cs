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
            SetInstrument( instrument);
            //Volume = instrument.volume;
            PlayScheduled(clip);
        }

        public void StopClip()
        {
            StopScheduled(1);
        }

    }
}