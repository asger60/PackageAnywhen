using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class AnywhenEventFunnel : MonoBehaviour
    {
        public void HandleNoteEvent(NoteEvent e, AnywhenInstrument anywhenSettings,
            AudioMixerGroup mixerChannel = null)
        {
            switch (anywhenSettings)
            {
                case AnywhenSampleInstrument instrumentObject:
                    AnywhenRuntime.AnywhenSamplerHandler.HandleEvent(e, instrumentObject, AnywhenMetronome.TickRate.Sub16,
                        mixerChannel);
                    break;
                case AnywhenSettingsScale settingsObjectScale:
                    AnywhenScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
                case AnywhenSynthPreset settingsSynth:
                    AnywhenRuntime.AnywhenSynthHandler.HandleEvent(e, settingsSynth, AnywhenMetronome.TickRate.Sub16);
                    break;
            }
        }

        public void HandleNoteEvent(NoteEvent e, AnywhenSettingsBase anywhenSettings,
            AnywhenMetronome.TickRate tickRate,
            AudioMixerGroup mixerChannel = null)
        {
            switch (anywhenSettings)
            {
                case AnywhenSampleInstrument instrumentObject:
                    AnywhenRuntime.AnywhenSamplerHandler.HandleEvent(e, instrumentObject, tickRate, mixerChannel);
                    break;

                case AnywhenSettingsScale settingsObjectScale:
                    AnywhenScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
                case AnywhenSynthPreset settingsSynth:
                    AnywhenRuntime.AnywhenSynthHandler.HandleEvent(e, settingsSynth, tickRate);
                    break;
            }
        }
    }
}