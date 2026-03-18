using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    [Serializable]
    public class AnywhenSampleVoice : AnywhenVoiceBase
    {
        private double _samplePosBuffer1;
        private double _sampleStepFrac;
        private AnywhenNoteClip _currentNoteClip;
        private readonly AnywhenSampleInstrument _thisInstrument;

        public AnywhenSampleVoice(AnywhenInstrument instrumentSettings, AnysongTrackSettings trackSettingsSettings) : base(instrumentSettings,
            trackSettingsSettings)
        {
            _thisInstrument = instrumentSettings as AnywhenSampleInstrument;
        }


        public override float GetDurationToEnd()
        {
            if (!_currentNoteClip) return 0;
            return base.GetDurationToEnd();
        }


        protected override void StartPlay(PlaybackSettings playbackSettings)
        {
            base.StartPlay(playbackSettings);
            var clipSettings = _thisInstrument.GetNoteClip(playbackSettings.note);
            _currentNoteClip = clipSettings.noteClip;
            if (_currentNoteClip == null) return;
            _samplePosBuffer1 = 0;
            _sampleStepFrac = _currentNoteClip.frequency / CurrentSampleRate;
            CurrentPitch = clipSettings.clipPitch;
            if (_thisInstrument.TempoControlPitch)
                CurrentPitch = _thisInstrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] data = new float[bufferSize];

            HandleQueue();


            //if (/*AmplitudeEnvelope.IsIdle && */!HasScheduledPlay)
            //{
            //    SetReady();
            //    return data;
            //}

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

        float[] DSP_WriteToBuffer(float[] data, int channels)
        {
            int i = 0;
            while (i < data.Length)
            {

                //ampMod *= AmplitudeEnvelope.Process();
                Double pitch = CurrentPitch;
                foreach (var pitchMod in currentTrackSettings.pitchMods)
                {
                    pitch = pitchMod.Process((float)pitch);
                }
                //if (CurrentTrack.pitchLFOSettings.enabled)
                //{
                //    PitchLFO.DoUpdate();
                //    pitch *= (PitchLFO.Process());
                //}

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

                    samples[c] = (float)val * _thisInstrument.volume * CurrentPlaybackSettings.volume;
                }

                for (int c = 0; c < channels; c++)
                {
                    if (i < data.Length)
                    {
                        data[i] = samples[c];
                        i++;
                    }
                }

                _samplePosBuffer1 += (_sampleStepFrac * pitch * currentTrackSettings.TrackPitch);
            }

            return data;
        }
    }
}