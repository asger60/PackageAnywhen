using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class EventFunnel : MonoBehaviour
    {
        
        public static void HandleNoteEvent(NoteEvent e, AnywhenSettingsBase anywhenSettings, AnywhenMetronome.TickRate tickRate,
            AudioMixerGroup mixerChannel = null)
        {
            
            print("handle event " + e.state);
            //if (instant) e.step = -1;
            switch (anywhenSettings)
            {
                case AnywhenInstrument instrumentObject:
                    SamplePlayer.Instance.HandleEvent(e, instrumentObject, tickRate, mixerChannel);
                    break;
                //case InstrumentObjectSynth synthSettings:
                //    SynthPlayer.Instance.HandleEvent(e, synthSettings);
                //    break;
                //case SettingsObjectEffect effectSettingsObject:
                //    EffectPlayer.Instance.HandleEvent(e, effectSettingsObject);
                //    break;

                //case JamModeObjectEffect effectSettingsObject:
                //    EffectPlayer.Instance.HandleEvent(e, effectSettingsObject);
                //    break;

                //case JamModeObjectPercussion objectPercussion:
                //    MultiSamplePlayer.Instance.HandleEvent(e, objectPercussion, mixerChannel);
                //    break;
                //
                //case JamModeObjectChromatic jamModeObjectChromatic:
                //    SamplePlayer.Instance.HandleEvent(e, jamModeObjectChromatic.instrumentObject, mixerChannel);
                //    break;
                //
                //case JamModeObjectString jamModeObjectString:
                //    SamplePlayer.Instance.HandleEvent(e, jamModeObjectString.instrumentObject, mixerChannel);
                //    break;


                case AnywhenSettingsScale settingsObjectScale:
                    ScalePlayer.Instance.HandleEvent(e, settingsObjectScale);
                    break;
            }
        }
    }
}