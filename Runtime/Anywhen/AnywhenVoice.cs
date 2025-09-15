using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenVoice 
    {
        public bool IsReady { get; private set; }
        public bool HasScheduledPlay => _hasScheduledPlay;
        private bool _hasScheduledPlay;

        //private AudioSource _audioSource;

        public AnywhenSampleInstrument Instrument => _currentPlaybackSettings.Instrument;

        public int CurrentNote => _currentPlaybackSettings.Note;

        private bool _isPlaying;


        public double ScheduledPlayTime => _currentPlaybackSettings.PlayTime;
        public bool IsPlaying => _isPlaying;


        private AnywhenNoteClip NoteClip => _currentPlaybackSettings.NoteClip;

        private ADSR _adsr = new();
        private bool UseEnvelope => _currentPlaybackSettings.Envelope.enabled;

        private float _bufferFadeValue, _buffer2FadeValue;


        private double _samplePosBuffer1 /*, _samplePosBuffer2*/;
        private double _sampleStepFrac;

        private double _currentPitch;

        private float _ampMod;

        public double SampleposBuffer1 => _samplePosBuffer1;

        [Serializable]
        struct PlaybackSettings
        {
            public double PlayTime, StopTime;
            public float Volume;
            public float Pitch;
            public int Note;
            public AnywhenSampleInstrument Instrument;

            public AnywhenSampleInstrument.EnvelopeSettings Envelope;

            //public AnysongTrack Track;
            public AnywhenNoteClip NoteClip;

            public PlaybackSettings(double playTime, double stopTime, float volume, float pitch, int note,
                AnywhenSampleInstrument instrument,
                AnywhenSampleInstrument.EnvelopeSettings envelope, AnywhenNoteClip noteClip, AnysongTrack track)
            {
                PlayTime = playTime;
                StopTime = stopTime;
                Volume = volume;
                Pitch = pitch;
                Note = note;
                Instrument = instrument;
                Envelope = envelope;
                //Track = track;
                NoteClip = noteClip;
            }
        }

        private PlaybackSettings _currentPlaybackSettings;
        private PlaybackSettings _nextPlaybackSettings;
        private float _currentSampleRate;

        public void Init(int currentSamleRate)
        {
            //AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            //TryGetComponent(out _audioSource);
            IsReady = true;
            //_audioSource.playOnAwake = true;
            //_audioSource.clip = myClip;
            _adsr = new ADSR();
            //_audioSource.Play();
            _currentSampleRate = currentSamleRate;
        }


        public void NoteOn(int note, double playTime, double stopTime, float volume, AnywhenSampleInstrument instrument,
            AnywhenSampleInstrument.EnvelopeSettings envelope, AnysongTrack track = null)
        {
            if (!instrument)
            {
                return;
            }

            var noteClip = instrument.GetNoteClip(note);
            if (noteClip)
            {
                if (track != null && track.trackEnvelope.enabled)
                {
                    envelope = track.trackEnvelope;
                }

                PlayScheduled(new PlaybackSettings(playTime, stopTime, volume, 1, note, instrument, envelope, noteClip, track));
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


        public float GetDurationToEnd()
        {
            if (!NoteClip) return 0;
            var timeToPlay = (float)(ScheduledPlayTime - AudioSettings.dspTime);
            timeToPlay = Mathf.Max(timeToPlay, 0);
            return timeToPlay + (float)(NoteClip.clipSamples.Length - _samplePosBuffer1);
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

            _sampleStepFrac = _currentPlaybackSettings.NoteClip.frequency / _currentSampleRate;
            _currentPitch = 1;
            if (Instrument.TempoControlPitch)
            {
                _currentPitch = Instrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());
            }

            _currentPlaybackSettings.Pitch = 1;

            _isPlaying = true;
            _hasScheduledPlay = false;

            SetEnvelope(_currentPlaybackSettings.Envelope);
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


        void OnAudioFilterRead(float[] data, int channels)
        {
        }


        float[] DSP_WriteToBuffer(float[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                _ampMod = 1;
                if (UseEnvelope)
                {
                    _ampMod *= _adsr.Process();
                }

                int sampleIndex1 = (int)_samplePosBuffer1;
                double f1 = _samplePosBuffer1 - sampleIndex1;
                var sourceSample1 = Mathf.Min((sampleIndex1), NoteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex1) + 1, NoteClip.clipSamples.Length - 1);
                double e1 = ((1 - f1) * NoteClip.clipSamples[sourceSample1]) +
                            (f1 * NoteClip.clipSamples[sourceSample2]);

                data[i] = ((float)(e1)) * _ampMod * _currentPlaybackSettings.Instrument.volume * _currentPlaybackSettings.Volume;

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

            _isPlaying = false;
        }

        protected void SetInstrument(AnywhenSampleInstrument instrument)
        {
            _currentPlaybackSettings.Instrument = instrument;
        }

        public float[] UpdateDSP()
        {
            float[] data = new float[2048];
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

            if (UseEnvelope && _adsr.IsIdle)
            {
                SetReady();
                return data;
            }


            if (_samplePosBuffer1 >= NoteClip.clipSamples.Length/* || _ampMod <= 0*/)
            {
                _adsr.SetGate(false);
                SetReady();
                return data;
            }

            return DSP_WriteToBuffer(data);
        }
    }
}