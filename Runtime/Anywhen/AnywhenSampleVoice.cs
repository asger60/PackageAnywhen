using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenSampleVoice : AnywhenVoiceBase
    {
        private double _samplePosBuffer1;
        private double _sampleStepFrac;


        private AnywhenNoteClip _currentNoteClip;


        AnywhenSampleInstrument _thisInstrument;


        public override void Init(int sampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings)
        {
            currentTrack = trackSettings;
            _thisInstrument = instrumentSettings as AnywhenSampleInstrument;
            adsr = new ADSR();
            pitchLFO = new SynthControlLFO();
            currentSampleRate = sampleRate;
        }


        public override float GetDurationToEnd()
        {
            if (!_currentNoteClip) return 0;
            if (playbackQueue.Count == 0) return 0;
            return (float)playbackQueue[^1].StopTime;
        }


        void StartPlay(PlaybackSettings playbackSettings)
        {
            CurrentPlaybackSettings = playbackSettings;
            _currentNoteClip = _thisInstrument.GetNoteClip(playbackSettings.Note);

            if (_currentNoteClip == null) return;
            _samplePosBuffer1 = 0;

            _sampleStepFrac = _currentNoteClip.frequency / currentSampleRate;

            currentPitch = 1;
            if (_thisInstrument.TempoControlPitch)
                currentPitch = _thisInstrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());


            isPlaying = true;
            adsr.Reset();
            adsr.SetGate(true);

            if (currentTrack.pitchLFOSettings is { enabled: true, retrigger: true }) pitchLFO.NoteOn();
        }


        float[] DSP_WriteToBuffer(float[] data, int channels)
        {
            int i = 0;
            while (i < data.Length)
            {
                float ampMod = 1;

                ampMod *= adsr.Process();

                if (currentTrack.pitchLFOSettings.enabled)
                {
                    pitchLFO.DoUpdate();
                    currentPitch = pitchLFO.Process();
                }

                int sampleIndex1 = (int)_samplePosBuffer1;
                double f1 = _samplePosBuffer1 - sampleIndex1;

                // Handle multichannel source clips
                int sourceChannels = _currentNoteClip.channels;
                float[] samples = new float[channels];

                for (int c = 0; c < channels; c++)
                {
                    // Map output channel to source channel
                    int sourceChannel = c % sourceChannels;
                    int sIndex1 = (sampleIndex1 * sourceChannels) + sourceChannel;
                    int sIndex2 = sIndex1 + sourceChannels;

                    // Clamp to the buffer size
                    sIndex1 = Mathf.Min(sIndex1, _currentNoteClip.clipSamples.Length - 1);
                    sIndex2 = Mathf.Min(sIndex2, _currentNoteClip.clipSamples.Length - 1);

                    double val = ((1 - f1) * _currentNoteClip.clipSamples[sIndex1]) +
                                 (f1 * _currentNoteClip.clipSamples[sIndex2]);

                    samples[c] = (float)val * ampMod * _thisInstrument.volume * CurrentPlaybackSettings.Volume;
                }

                for (int c = 0; c < channels; c++)
                {
                    if (i < data.Length)
                    {
                        data[i] = samples[c];
                        i++;
                    }
                }

                _samplePosBuffer1 += (_sampleStepFrac * currentPitch);
            }

            return data;
        }


        private void SetReady()
        {
            adsr.SetGate(false);
            isPlaying = false;
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] data = new float[bufferSize];

            while (playbackQueue.Count > 0 && AudioSettings.dspTime >= playbackQueue[0].PlayTime)
            {
                StartPlay(playbackQueue[0]);
                playbackQueue.RemoveAt(0);
            }


            if (AudioSettings.dspTime >= CurrentPlaybackSettings.StopTime)
            {
                adsr.SetGate(false);
            }


            if (adsr.IsIdle)
            {
                SetReady();
                return data;
            }

            if (!_currentNoteClip)
            {
                SetReady();
                return data;
            }


            if (_samplePosBuffer1 >= _currentNoteClip.clipSamples.Length / (float)_currentNoteClip.channels)
            {
                SetReady();
                return data;
            }

            return DSP_WriteToBuffer(data, channels);
        }
    }
}