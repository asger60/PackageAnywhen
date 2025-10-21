using Anywhen;
using Anywhen.SettingsObjects;

public class AnywhenComposerPlayer : AnywhenPlayerBase
{

    public override void Play()
    {
        base.Play();
        var section = CurrentSong.Sections[CurrentSong.CurrentSectionIndex];
        AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
    }

    protected override void OnBar()
    {
        if (!IsRunning) return;
        base.OnBar();
        var section = CurrentSong.Sections[CurrentSong.CurrentSectionIndex];
        AnywhenRuntime.Conductor.SetScaleProgression(section.GetProgressionStep(CurrentBar, CurrentSong.Sections[0]));
    }
}