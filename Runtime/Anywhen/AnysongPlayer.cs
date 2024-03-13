using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


public class AnysongPlayer : MonoBehaviour
{
    public AnysongObject songObject;
    private AnywhenInstrument[] _instruments;
    private AnysongObject _currentSong;
    private bool _isRunning;
    private bool _loaded = false;
    public bool IsSongLoaded => _loaded;
    public float intensity;
    public AnysongPlayerBrain.TransitionTypes triggerTransitionsType;

    private void Start()
    {
        Load(songObject);
    }

    private void Load(AnysongObject anysong)
    {
        _loaded = true;
        _currentSong = anysong;

        foreach (var track in anysong.Tracks)
        {
            if (track.instrument is AnywhenSynthPreset preset)
            {
                AnywhenRuntime.AnywhenSynthHandler.RegisterPreset(preset);
            }
        }
    }

    private NoteEvent[] _lastTrackNote;


    //[Serializable]
    //struct TrackStep
    //{
    //    public float distance;
    //    public int trackIndex;
    //    public AnyPatternStep step;
//
    //    public TrackStep(AnyPatternStep step, float distance, int trackIndex)
    //    {
    //        this.distance = distance;
    //        this.step = step;
    //        this.trackIndex = trackIndex;
    //    }
    //}
//
    //private List<TrackStep> _trackSteps = new List<TrackStep>();

    private void OnTick16()
    {
        if (!_isRunning) return;

        if (_currentSong != songObject)
        {
            Release();
            Load(songObject);
        }

        int stepIndex = AnywhenRuntime.Metronome.Sub16;

        //_trackSteps.Clear();
//
//
        //for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        //{
        //    for (int sectionIndex = 0; sectionIndex < _currentSong.Sections.Count; sectionIndex++)
        //    {
        //        var thisPattern = _currentSong.Sections[sectionIndex].tracks[trackIndex]
        //            .GetPattern(AnywhenMetronome.Instance.CurrentBar);
//
        //        float dist = MathF.Abs(intensity - (thisPattern.steps[stepIndex].mixWeight + sectionIndex));
        //        _trackSteps.Add(new TrackStep(thisPattern.steps[stepIndex], dist, trackIndex));
        //    }
        //}


        for (int trackIndex = 0; trackIndex < _currentSong.Tracks.Count; trackIndex++)
        {
            var track = _currentSong.Sections[0].tracks[trackIndex];
            if (track.isMuted) continue;
            
            var songTrack = _currentSong.Tracks[trackIndex];
            AnywhenRuntime.Conductor.SetScaleProgression(_currentSong.Sections[0]
                .GetProgressionStep(AnywhenMetronome.Instance.CurrentBar));

            var pattern = track.GetPattern(AnywhenMetronome.Instance.CurrentBar);
            var step = pattern.steps[stepIndex];
            float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(intensity));

            float thisRnd = Random.Range(0, 1f);

            if (thisRnd < step.chance && step.mixWeight < thisIntensity)
            {
                if (step.noteOn || step.noteOff)
                    songTrack.TriggerNoteOn(step, pattern);
            }
        }
    }

    public int GetStepForTrack(int trackIndex)
    {
        return AnywhenRuntime.Metronome.Sub16;
    }


    private void Release()
    {
        _loaded = false;
        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
    }


    public void SetMixIntensity(float value)
    {
    }

    public void ReleaseFromMetronome()
    {
        _isRunning = false;
        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
    }

    public void AttachToMetronome()
    {
        if (_isRunning) return;
        if (!_loaded) return;
        _isRunning = true;
        AnywhenRuntime.Metronome.OnTick16 += OnTick16;
    }

    public void Play()
    {
        AnysongPlayerBrain.TransitionTo(this, triggerTransitionsType);
    }

    public float GetTrackProgress()
    {
        int trackLength = _currentSong.Sections[0].patternSteps.Length;
        int currentBar = (int)Mathf.Repeat(AnywhenMetronome.Instance.CurrentBar, trackLength);
        return (float)currentBar / trackLength;
    }

    public void SetGlobalIntensity(float globalIntensity)
    {
        intensity = globalIntensity;
    }
}