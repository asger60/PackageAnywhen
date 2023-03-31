using System;
using PackageAnywhen.Runtime.Anywhen;
using Rytmos.AudioSystem;
using UnityEngine;

public class SamplePatternPlayer : MonoBehaviour
{
    [Serializable]
    public struct PatternTrack
    {
        public AnywhenInstrument anywhenInstrument;
        public PerformerObjectBase anywhenPerformerSettings;
        internal int StepIndex;
        internal int NoteIndex;
        public bool[] steps;
    }

    public PatternTrack[] patternTracks;

    private void Start()
    {
        AnywhenMetronome.Instance.OnTick16 += OnTick16;
    }

    private void OnTick16()
    {
        for (var i = 0; i < patternTracks.Length; i++)
        {
            var track = patternTracks[i];
            patternTracks[i].StepIndex++;
            patternTracks[i].StepIndex = (int)Mathf.Repeat(patternTracks[i].StepIndex, patternTracks[i].steps.Length);
            if (track.steps[(int)Mathf.Repeat(patternTracks[i].StepIndex , patternTracks[i].steps.Length)])
            {
                track.anywhenPerformerSettings.Play(track.NoteIndex, track.anywhenInstrument);
                patternTracks[i].NoteIndex++;
            }
        }
    }
}