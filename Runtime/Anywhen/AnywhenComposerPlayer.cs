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
}