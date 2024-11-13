using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenEventFunnel : MonoBehaviour
    {
        public void HandleNoteEvent(NoteEvent e, AnywhenInstrument anywhenSettings, AnysongTrack track = null)
        {
            switch (anywhenSettings)
            {
                case AnywhenSampleInstrument instrumentObject:
                    AnywhenRuntime.AnywhenSamplerHandler.HandleEvent(e, instrumentObject, AnywhenMetronome.TickRate.Sub16, track);
                    break;
                case AnywhenSettingsScale settingsObjectScale:
                    AnywhenScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
                case AnywhenSynthPreset settingsSynth:
                    AnywhenRuntime.AnywhenSynthHandler.HandleEvent(e, settingsSynth, AnywhenMetronome.TickRate.Sub16);
                    break;
            }
        }

        public void HandleNoteEvent(NoteEvent e, AnywhenSettingsBase anywhenSettings, AnywhenMetronome.TickRate tickRate, AnysongTrack track)
        {
            switch (anywhenSettings)
            {
                case AnywhenSampleInstrument instrumentObject:
                    AnywhenRuntime.AnywhenSamplerHandler.HandleEvent(e, instrumentObject, tickRate, track);
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