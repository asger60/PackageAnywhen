using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenSampleVoice : AnywhenVoiceBase
    {
        private ADSR _adsr = new();


        private double _samplePosBuffer1;
        private double _sampleStepFrac;

        private double _currentPitch;

        private float _ampMod;

        private AnywhenNoteClip _currentNoteClip;
        private SynthControlLFO _pitchLFO;

        private float _currentSampleRate;
        private AnysongTrack _currentTrack;
        private SynthSettingsObjectLFO _pitchSettings;

        public override void Init(int currentSampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings)
        {
            _currentTrack = trackSettings;
            _thisInstrument = instrumentSettings as AnywhenSampleInstrument;
            IsReady = true;
            _adsr = new ADSR();
            _pitchLFO = new SynthControlLFO();
            _currentSampleRate = currentSampleRate;
        }


        AnywhenSampleInstrument _thisInstrument;

        public override void NoteOn(PlaybackSettings playbackSettings)
        {
            if (AudioSettings.dspTime > playbackSettings.PlayTime) return;
            SetPitchLFO(_currentTrack.pitchLFOSettings);
            SetEnvelope(_currentTrack.trackEnvelope);

            playbackQueue.Add(playbackSettings);
            IsReady = false;
        }


        public override float GetDurationToEnd()
        {
            if (!_currentNoteClip) return 0;
            if (playbackQueue.Count == 0) return 0;
            return (float)playbackQueue[0].PlayTime;
        }


        void InitPlay(PlaybackSettings playbackSettings)
        {
            CurrentPlaybackSettings = playbackSettings;
            _currentNoteClip = _thisInstrument.GetNoteClip(playbackSettings.Note);

            if (_currentNoteClip == null) return;
            _samplePosBuffer1 = 0;

            _sampleStepFrac = _currentNoteClip.frequency / _currentSampleRate;

            _currentPitch = 1;
            if (_thisInstrument.TempoControlPitch)
                _currentPitch = _thisInstrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());


            isPlaying = true;
            _adsr.Reset();
            _adsr.SetGate(true);

            if (_currentTrack.pitchLFOSettings is { enabled: true, retrigger: true }) _pitchLFO.NoteOn();
        }


        void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * _currentSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * _currentSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * _currentSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }

        void SetPitchLFO(AnywhenSampleInstrument.PitchLFOSettings pitchLFOSettings)
        {
            _pitchLFO.UpdateSettings(pitchLFOSettings);
        }


        float[] DSP_WriteToBuffer(float[] data, int channels)
        {
            int i = 0;
            while (i < data.Length)
            {
                _ampMod = 1;

                _ampMod *= _adsr.Process();

                if (_currentTrack.pitchLFOSettings.enabled)
                {
                    _pitchLFO.DoUpdate();
                    _currentPitch = _pitchLFO.Process();
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

                    samples[c] = (float)val * _ampMod * _thisInstrument.volume * CurrentPlaybackSettings.Volume;
                }

                for (int c = 0; c < channels; c++)
                {
                    if (i < data.Length)
                    {
                        data[i] = samples[c];
                        i++;
                    }
                }

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch);
            }

            return data;
        }


        private void SetReady()
        {
            _adsr.SetGate(false);
            IsReady = true;
            isPlaying = false;
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] data = new float[bufferSize];

            while (playbackQueue.Count > 0 && AudioSettings.dspTime >= playbackQueue[0].PlayTime)
            {
                InitPlay(playbackQueue[0]);
                playbackQueue.RemoveAt(0);
            }


            if (AudioSettings.dspTime >= CurrentPlaybackSettings.StopTime)
            {
                _adsr.SetGate(false);
            }


            if (_adsr.IsIdle)
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