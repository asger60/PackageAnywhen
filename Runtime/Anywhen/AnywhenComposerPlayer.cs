using UnityEngine;

namespace Anywhen
{
    [ExecuteInEditMode]
    public class AnywhenComposerPlayer : AnywhenPlayerBase
    {
        public override void Play(bool syncToGlobalClock = false)
        {
            base.Play(syncToGlobalClock);
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
}