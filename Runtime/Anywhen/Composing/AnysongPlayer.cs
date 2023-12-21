using System;
using System.Collections.Generic;
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

    public float variation;


    private void Start()
    {
        Load(songObject);
    }

    public void Load(AnySongObject anySong)
    {
        _loaded = true;
        _currentSong = anySong;
        _isRunning = true;

        //_lastTrackNote = new NoteEvent[_instruments.Length];
        //for (var i = 0; i < _lastTrackNote.Length; i++)
        //{
        //    _lastTrackNote[i] = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
        //}

        AnywhenMetronome.Instance.OnTick16 += OnTick16;
    }

    private NoteEvent[] _lastTrackNote;


    [Serializable]
    struct TrackStep
    {
        public float distance;
        public int trackIndex;
        public AnyPatternStep step;

        public TrackStep(AnyPatternStep step, float distance, int trackIndex)
        {
            this.distance = distance;
            this.step = step;
            this.trackIndex = trackIndex;
        }
    }

    private List<TrackStep> _trackSteps = new List<TrackStep>();

    private void OnTick16()
    {
        if (!_isRunning) return;

        int step = AnywhenRuntime.Metronome.Sub16;

        _trackSteps.Clear();


        for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        {
            for (int sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
            {
                var thisPattern = _currentSong.Sections[sectionIndex].tracks[trackIndex]
                    .GetPattern(AnywhenMetronome.Instance.CurrentBar);

                float dist = MathF.Abs(variation - (thisPattern.steps[step].mixWeight + sectionIndex));
                _trackSteps.Add(new TrackStep(thisPattern.steps[step], dist, trackIndex));
            }
        }


        for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        {
            float bestDistance = float.MaxValue;
            TrackStep bestStep = _trackSteps[0];
            foreach (var trackStep in _trackSteps)
            {
                if (trackStep.trackIndex != trackIndex) continue;
                if (trackStep.distance < bestDistance)
                {
                    bestDistance = trackStep.distance;
                    bestStep = trackStep;
                }
            }

            bestStep.step.TriggerStep(_currentSong.Tracks[bestStep.trackIndex]);
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