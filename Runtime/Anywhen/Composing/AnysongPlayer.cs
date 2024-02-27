using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


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
        foreach (var track in anySong.Tracks)
        {
            if (track.instrument is AnywhenSynthPreset preset)
            {
                AnywhenRuntime.AnywhenSynthHandler.RegisterPreset(preset);
            }
        }

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

        if (_currentSong != songObject)
        {
            Release();
            Load(songObject);
        }

        int stepIndex = AnywhenRuntime.Metronome.Sub16;

        _trackSteps.Clear();


        for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        {
            for (int sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
            {
                var thisPattern = _currentSong.Sections[sectionIndex].tracks[trackIndex].GetPattern(AnywhenMetronome.Instance.CurrentBar);

                float dist = MathF.Abs(variation - (thisPattern.steps[stepIndex].mixWeight + sectionIndex));
                _trackSteps.Add(new TrackStep(thisPattern.steps[stepIndex], dist, trackIndex));
            }
        }


        for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        {
            //float bestDistance = float.MaxValue;
            //TrackStep bestStep = _trackSteps[0];
            //foreach (var trackStep in _trackSteps)
            //{
            //    if (trackStep.trackIndex != trackIndex) continue;
            //    if (trackStep.distance < bestDistance)
            //    {
            //        bestDistance = trackStep.distance;
            //        bestStep = trackStep;
            //    }
            //}
            //if (Random.Range(0, 1f) < bestStep.step.chance)
            //    bestStep.step.TriggerStep(_currentSong.Tracks[bestStep.trackIndex]);
            var track = _currentSong.Sections[0].tracks[trackIndex];
            var step = track.GetPattern(AnywhenMetronome.Instance.CurrentBar).steps[stepIndex];
            if (Random.Range(0, 1f) < step.chance)
            {
                step.TriggerStep(_currentSong.Tracks[trackIndex]);
            }
        }
    }

    public int GetStepForTrack(int trackIndex)
    {
        return AnywhenRuntime.Metronome.Sub16;
    }


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