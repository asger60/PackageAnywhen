using Anywhen;
using Anywhen.SettingsObjects;

public class AnywhenComposerPlayer : AnywhenPlayerBase
{
#if UNITY_EDITOR
    public void LoadInstruments()
    {
        foreach (var track in CurrentSong.Tracks)
        {
            if (track.instrument is AnywhenSampleInstrument instrument)
            {
                InstrumentDatabase.LoadInstrumentNotes(instrument);
            }
        }
    }
#endif

    public override void Play()
    {
        base.Play();
        var section = CurrentSong.Sections[CurrentSectionIndex];
        AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
    }

    protected override void OnBar()
    {
        if (!IsRunning) return;
        base.OnBar();
        var section = CurrentSong.Sections[CurrentSectionIndex];
        AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
    }
}