using Anywhen;
using Anywhen.SettingsObjects;
using UnityEngine;

public class AnysongPlayer : MonoBehaviour
{
    public AnySongObject songObject;
    private AnywhenInstrument[] _instruments;
    private AnySongObject _currentSong;
    private bool _isRunning;
    private bool _loaded;


    private void Start()
    {
        Load(songObject);
    }

    public void Load(AnySongObject anySong)
    {
        _loaded = true;
        _currentSong = anySong;

        //_lastTrackNote = new NoteEvent[_instruments.Length];
        //for (var i = 0; i < _lastTrackNote.Length; i++)
        //{
        //    _lastTrackNote[i] = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
        //}

        AnywhenMetronome.Instance.OnTick16 += OnTick16;
    }

    private NoteEvent[] _lastTrackNote;

    private void OnTick16()
    {
        // if (!_isRunning) return;

        int step = AnywhenRuntime.Metronome.Sub16;


        for (int sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
        {
            for (int trackIndex = 0; trackIndex < _currentSong.Sections[sectionIndex].tracks.Count; trackIndex++)
            {
                var thisPattern = _currentSong.Sections[sectionIndex].tracks[trackIndex]
                    .GetPattern(AnywhenMetronome.Instance.CurrentBar);
                
                thisPattern.steps[step].TriggerStep(_currentSong.Sections[sectionIndex].tracks[trackIndex]);
            }
        }
    }


    //AnySection.AnyPatternStep GetStep(int sub16, int track)
    //{
    //    AnySection.AnyPatternStep step = new AnySection.AnyPatternStep();
    //    float bestDistance = float.MaxValue;
    //    for (var i = 0; i < _currentSong.Sections.Count; i++)
    //    {
    //        var pattern = _currentSong.Sections[i].GetPattern(AnywhenMetronome.Instance.CurrentBar, track);
    //        
    //        float selectorDistance = Mathf.Abs(
    //            ((0.5f - pattern.tracks[track].steps[sub16].mixWeight) + i) -
    //            _musicMixerPlayer.GetVariationValue());
//
    //        if (selectorDistance > 1) continue;
    //        if (selectorDistance < bestDistance)
    //        {
    //            bestDistance = selectorDistance;
    //            step = pattern.tracks[track].steps[sub16];
    //        }
    //    }
//
    //    return step;
    //}


    public void Release()
    {
        _loaded = false;
        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
    }


    public void Stop()
    {
        _isRunning = false;
        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
    }

    public void Play()
    {
        if (_isRunning) return;
        if (!_loaded) return;
        _isRunning = true;
        AnywhenRuntime.Metronome.OnTick16 += OnTick16;
    }
}