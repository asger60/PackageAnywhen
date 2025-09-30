using System;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenSampleVoice : AnywhenVoiceBase
    {
        //private AudioSource _audioSource;


        public int CurrentNote => _currentPlaybackSettings.Note;


        public double ScheduledPlayTime => _currentPlaybackSettings.PlayTime;


        private ADSR _adsr = new();

        private float _bufferFadeValue, _buffer2FadeValue;


        private double _samplePosBuffer1 /*, _samplePosBuffer2*/;
        private double _sampleStepFrac;

        private double _currentPitch;

        private float _ampMod;

        public double SampleposBuffer1 => _samplePosBuffer1;

        AnywhenNoteClip _currentNoteClip;

        private float _currentSampleRate;

        public override void Init(int currentSampleRate, AnywhenInstrument instrument, AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _thisInstrument = instrument as AnywhenSampleInstrument;
            _envelope = envelopeSettings;
            IsReady = true;
            _adsr = new ADSR();
            _currentSampleRate = currentSampleRate;
        }

        AnywhenSampleInstrument _thisInstrument;
        AnywhenSampleInstrument.EnvelopeSettings _envelope;


        public override void NoteOn(int note, double playTime, double stopTime, float volume)
        {
            if (AudioSettings.dspTime > playTime)
            {
                AnywhenRuntime.Log("Trying to schedule a play at a time that has allready been..", AnywhenRuntime.DebugMessageType.Warning);
                return;
            }

            if (note > 20)
            {
                AnywhenRuntime.Log("note value too high", AnywhenRuntime.DebugMessageType.Warning);

                return;
            }

            _currentNoteClip = _thisInstrument.GetNoteClip(note);
            if (_currentNoteClip)
            {
                PlayScheduled(new PlaybackSettings(playTime, stopTime, volume, 1, note));
                if (stopTime > 0) StopScheduled(stopTime);
            }
            else
            {
                AnywhenRuntime.Log("failed to find NoteClip", AnywhenRuntime.DebugMessageType.Warning);
            }
        }


        public void NoteOff(double stopTime)
        {
            StopScheduled(stopTime);
        }


        public override float GetDurationToEnd()
        {
            var timeToPlay = (float)(ScheduledPlayTime - AudioSettings.dspTime);
            timeToPlay = Mathf.Max(timeToPlay, 0);
            return timeToPlay + (float)(_currentNoteClip.clipSamples.Length - _samplePosBuffer1);
        }


        protected void StopScheduled(double absoluteTime)
        {
            _nextPlaybackSettings.StopTime = absoluteTime;
        }

        public void SetPitch(float pitchValue)
        {
            _currentPlaybackSettings.Pitch = pitchValue;
        }


        private void PlayScheduled(PlaybackSettings nextUp)
        {
            _nextPlaybackSettings = nextUp;
            _hasScheduledPlay = true;
            IsReady = false;
        }


        void InitPlay()
        {
            _currentPlaybackSettings = _nextPlaybackSettings;
            _samplePosBuffer1 = 0;

            _sampleStepFrac = _currentNoteClip.frequency / _currentSampleRate;
            _currentPitch = 1;
            if (_thisInstrument.TempoControlPitch)
            {
                _currentPitch = _thisInstrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());
            }

            _currentPlaybackSettings.Pitch = 1;

            _isPlaying = true;
            _hasScheduledPlay = false;

            SetEnvelope(_envelope);
            _adsr.SetGate(true);
        }

        void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * _currentSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * _currentSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * _currentSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }


        float[] DSP_WriteToBuffer(float[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                _ampMod = 1;

                _ampMod *= _adsr.Process();


                int sampleIndex1 = (int)_samplePosBuffer1;
                double f1 = _samplePosBuffer1 - sampleIndex1;
                var sourceSample1 = Mathf.Min((sampleIndex1), _currentNoteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex1) + 1, _currentNoteClip.clipSamples.Length - 1);
                double e1 = ((1 - f1) * _currentNoteClip.clipSamples[sourceSample1]) +
                            (f1 * _currentNoteClip.clipSamples[sourceSample2]);

                data[i] = ((float)(e1)) * _ampMod * _thisInstrument.volume * _currentPlaybackSettings.Volume;

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;

                //_currentPitch = (Mathf.MoveTowards((float)_currentPitch, _pitch, 0.001f));


                i++;
            }

            return data;
        }

        //void DSP_HandleLooping()
        //{
        //    if ((int)_samplePosBuffer1 >= _currentLoopSettings.loopStart)
        //    {
        //        _samplePosBuffer1 = (_currentLoopSettings.loopStart - _currentLoopSettings.loopLength) *
        //                            (_sampleStepFrac * _currentPitch);
        //    }
        //}


        private void SetReady()
        {
            IsReady = true;
            _hasScheduledPlay = false;
            _currentPlaybackSettings.Note = -1000;
            _currentNoteClip = null;
            _isPlaying = false;
        }

        protected void SetInstrument(AnywhenSampleInstrument instrument)
        {
            _thisInstrument = instrument;
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] data = new float[bufferSize];
            if (_hasScheduledPlay && AudioSettings.dspTime >= _nextPlaybackSettings.PlayTime)
            {
                InitPlay();
            }

            if (!_isPlaying) return data;

            if (_currentPlaybackSettings.StopTime >= 0 && AudioSettings.dspTime > _currentPlaybackSettings.StopTime)
            {
                _currentPlaybackSettings.StopTime = -1;
                _adsr.SetGate(false);
            }


            //if (_isLooping)
            //{
            //    DSP_HandleLooping();
            //}

            if (_adsr.IsIdle)
            {
                SetReady();
                return data;
            }


            if (_samplePosBuffer1 >= _currentNoteClip.clipSamples.Length /* || _ampMod <= 0*/)
            {
                _adsr.SetGate(false);
                SetReady();
                return data;
            }

            return DSP_WriteToBuffer(data);
        }
    }
}