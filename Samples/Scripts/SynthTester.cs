using Anywhen;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnitySynth.Runtime.AudioSystem;

public class SynthTester : MonoBehaviour
{
    public AnywhenSettingsBase synthPreset;


    private void Start()
    {
        AnywhenSynthPreset preset = synthPreset as AnywhenSynthPreset;
        if (preset != null)
            AnywhenRuntime.AnywhenSynthHandler.RegisterPreset(preset);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            AnywhenRuntime.EventFunnel.HandleNoteEvent(new NoteEvent(Random.Range(20, 30)), synthPreset, AnywhenMetronome.TickRate.None);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            AnywhenRuntime.EventFunnel.HandleNoteEvent(new NoteEvent(NoteEvent.EventTypes.NoteOff), synthPreset, AnywhenMetronome.TickRate.None);
        }
    }
}