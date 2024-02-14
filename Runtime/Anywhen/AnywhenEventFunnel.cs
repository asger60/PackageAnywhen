using System;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class AnywhenEventFunnel : MonoBehaviour
    {
        
        public void HandleNoteEvent(NoteEvent e, AnywhenSettingsBase anywhenSettings, AudioMixerGroup mixerChannel = null)
        {
            
            //if (instant) e.step = -1;
            switch (anywhenSettings)
            {
                case AnywhenInstrument instrumentObject:
                    AnywhenSamplePlayer.Instance.HandleEvent(e, instrumentObject, AnywhenMetronome.TickRate.Sub16, mixerChannel);
                    break;

                case AnywhenSettingsScale settingsObjectScale:
                    ScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
            }
        }
        
        public void HandleNoteEvent(NoteEvent e, AnywhenSettingsBase anywhenSettings, AnywhenMetronome.TickRate tickRate,
            AudioMixerGroup mixerChannel = null)
        {
            
            //if (instant) e.step = -1;
            switch (anywhenSettings)
            {
                case AnywhenInstrument instrumentObject:
                    AnywhenSamplePlayer.Instance.HandleEvent(e, instrumentObject, tickRate, mixerChannel);
                    break;

                case AnywhenSettingsScale settingsObjectScale:
                    ScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
            }
        }
    }
}