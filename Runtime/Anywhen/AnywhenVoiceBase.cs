using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [Serializable]
    public abstract class AnywhenVoiceBase
    {
        public bool IsReady => _playbackQueue.Count == 0 && !IsPlaying;

        protected AnysongTrackSettings currentTrackSettings;

        //protected ADSR AmplitudeEnvelope;
        protected bool IsPlaying;
        public bool HasScheduledPlay => _playbackQueue.Count > 0;

        private List<PlaybackSettings> _playbackQueue = new();

        protected double CurrentPitch;
        protected float CurrentSampleRate;


        [Serializable]
        public struct PlaybackSettings
        {
            public double playTime;
            public double stopTime;
            public float volume;
            public int note;
        }

        protected PlaybackSettings CurrentPlaybackSettings;

        protected AnywhenVoiceBase(AnywhenInstrument instrumentSettings, AnysongTrackSettings trackSettingsSettings)
        {
            CurrentSampleRate = AudioSettings.outputSampleRate;
            currentTrackSettings = trackSettingsSettings;
        }


        public void NoteOn(PlaybackSettings playbackSettings)
        {
            if (AudioSettings.dspTime > playbackSettings.playTime)
            {
                return;
            }

            IsPlaying = true;
            _playbackQueue.Add(playbackSettings);
        }


        public virtual float GetDurationToEnd()
        {
            if (_playbackQueue.Count == 0) return 0;
            return (float)_playbackQueue[^1].stopTime;
        }

        protected virtual void StartPlay(PlaybackSettings playbackSettings)
        {
            IsPlaying = true;
            CurrentPlaybackSettings = playbackSettings;
            CurrentPitch = 1;
        }

        protected void HandleQueue()
        {
            while (_playbackQueue.Count > 0 && AudioSettings.dspTime >= _playbackQueue[0].playTime)
            {
                StartPlay(_playbackQueue[0]);
                _playbackQueue.RemoveAt(0);
            }

            if (AudioSettings.dspTime >= CurrentPlaybackSettings.stopTime)
            {
                SetReady();
            }
        }

        public abstract float[] UpdateDSP(int bufferSize, int channels);

        protected virtual void SetReady()
        {
            IsPlaying = false;
        }
    }
}