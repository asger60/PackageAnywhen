using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenSampler : MonoBehaviour
    {
        public bool IsReady { get; private set; }
        public bool IsArmed => _isArmed;
        private bool _isArmed;

        private AudioSource _audioSource;


        public AnywhenSampleInstrument Instrument => _playbackSettings.Instrument;

        //private AnywhenSampleInstrument _instrument;
        public bool IsStopping => _isStopping;
        private bool _isStopping;

        public int CurrentNote => _playbackSettings.Note;

        //private int _currentNote;
        //protected float Volume;
        //private AnysongTrack _track;

        //private bool _isLooping;

        private bool _isPlaying;
        private bool _scheduledPlay;
        public double ScheduledPlayTime => _playbackSettings.PlayTime;
        public bool IsPlaying => _isPlaying;

        //private double _scheduledPlayTime = -1;
        //private double _scheduledStopTime;
        private AnywhenNoteClip _noteClip;
        private ADSR _adsr = new();
        private bool UseEnvelope => _playbackSettings.Envelope.enabled;

        //private AnywhenSampleInstrument.LoopSettings _currentLoopSettings;
        private float _bufferFadeValue, _buffer2FadeValue;


        private double _samplePosBuffer1 /*, _samplePosBuffer2*/;
        private double _sampleStepFrac;

        private double _currentPitch;

        //private float _pitch;
        private float _ampMod;

        public double SampleposBuffer1 => _samplePosBuffer1;

        struct PlaybackSettings
        {
            public double PlayTime, StopTime;
            public float Volume;
            public float Pitch;
            public int Note;
            public AnywhenSampleInstrument Instrument;
            public AnywhenSampleInstrument.EnvelopeSettings Envelope;
            public AnysongTrack Track;
            public AnywhenNoteClip NoteClip;

            public PlaybackSettings(double playTime, double stopTime, float volume, float pitch, int note, AnywhenSampleInstrument instrument,
                AnywhenSampleInstrument.EnvelopeSettings envelope, AnywhenNoteClip noteClip, AnysongTrack track)
            {
                PlayTime = playTime;
                StopTime = stopTime;
                Volume = volume;
                Pitch = pitch;
                Note = note;
                Instrument = instrument;
                Envelope = envelope;
                Track = track;
                NoteClip = noteClip;
            }
        }

        private PlaybackSettings _playbackSettings;

        public void Init()
        {
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            TryGetComponent(out _audioSource);
            IsReady = true;
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
            _adsr = new ADSR();
            _audioSource.Play();
        }


        public void NoteOn(int note, double playTime, double stopTime, float volume, AnywhenSampleInstrument instrument,
            AnywhenSampleInstrument.EnvelopeSettings envelope, AnysongTrack track = null)
        {
            SetReady(false);
            if (instrument == null)
            {
                SetReady(true);
                return;
            }


            var noteClip = instrument.GetNoteClip(note);
            if (noteClip != null)
            {
                if (track != null && track.trackEnvelope.enabled)
                {
                    envelope = track.trackEnvelope;
                }


                _playbackSettings = new PlaybackSettings(playTime, stopTime, volume, 1, note, instrument, envelope, noteClip, track);

                _isArmed = true;
                _audioSource.Stop();
                SetEnvelope(envelope);
                PlayScheduled(noteClip);
                if (stopTime > 0)
                {
                    StopScheduled(stopTime);
                }
            }
            else
            {
                AnywhenRuntime.Log("failed to find NoteClip", AnywhenRuntime.DebugMessageType.Warning);
                SetReady(true);
            }
        }


        public void NoteOff(double stopTime)
        {
            _isStopping = true;
            StopScheduled(stopTime);
        }


        void Reset()
        {
            _isStopping = false;
            _playbackSettings.Instrument = null;
            IsReady = true;
            _isArmed = false;
        }

        public float GetDurationToEnd()
        {
            var timeToPlay = (float)(ScheduledPlayTime - AudioSettings.dspTime);
            timeToPlay = Mathf.Max(timeToPlay, 0);
            return timeToPlay - (float)(_noteClip.clipSamples.Length - _samplePosBuffer1);
        }


        protected void StopScheduled(double absoluteTime)
        {
            _playbackSettings.StopTime = absoluteTime;
        }

        public void SetPitch(float pitchValue)
        {
            _playbackSettings.Pitch = pitchValue;
        }

        private void PlayScheduled(AnywhenNoteClip clip)
        {
            _samplePosBuffer1 = 0;
            _noteClip = clip;
            _scheduledPlay = true;
            _sampleStepFrac = clip.frequency / (float)AudioSettings.outputSampleRate;
            _currentPitch = 1;
            _playbackSettings.Pitch = 1;
            

            _playbackSettings.StopTime = -1;
        }

        void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * AudioSettings.outputSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * AudioSettings.outputSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * AudioSettings.outputSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }


        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_noteClip == null)
            {
                return;
            }

            if (!_isPlaying && _scheduledPlay && AudioSettings.dspTime >= _playbackSettings.PlayTime)
            {
                _isPlaying = true;
                _scheduledPlay = false;
                _isArmed = false;
                _adsr.SetGate(true);
            }

            if (!_isPlaying) return;

            if (_playbackSettings.StopTime >= 0 && AudioSettings.dspTime > _playbackSettings.StopTime)
            {
                _playbackSettings.StopTime = -1;
                _adsr.SetGate(false);
                //_isLooping = false;
            }

            DSP_WriteToBuffer(data);


            //if (_isLooping)
            //{
            //    DSP_HandleLooping();
            //}

            if (UseEnvelope && _adsr.IsIdle)
            {
                SetReady(true);
            }


            if (_samplePosBuffer1 >= _noteClip.clipSamples.Length || _ampMod <= 0)
            {
                _adsr.SetGate(false);
                SetReady(true);
            }
        }


        void DSP_WriteToBuffer(float[] data)
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
                var sourceSample1 = Mathf.Min((sampleIndex1), _noteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex1) + 1, _noteClip.clipSamples.Length - 1);
                double e1 = ((1 - f1) * _noteClip.clipSamples[sourceSample1]) +
                            (f1 * _noteClip.clipSamples[sourceSample2]);

                data[i] = ((float)(e1)) * _ampMod * _playbackSettings.Instrument.volume * _playbackSettings.Volume;

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;

                //_currentPitch = (Mathf.MoveTowards((float)_currentPitch, _pitch, 0.001f));


                i++;
            }
        }

        //void DSP_HandleLooping()
        //{
        //    if ((int)_samplePosBuffer1 >= _currentLoopSettings.loopStart)
        //    {
        //        _samplePosBuffer1 = (_currentLoopSettings.loopStart - _currentLoopSettings.loopLength) *
        //                            (_sampleStepFrac * _currentPitch);
        //    }
        //}


        public void SetReady(bool state)
        {
            IsReady = state;
            if (state)
            {
                _isArmed = false;
                _playbackSettings.Note = -1000;
                _isPlaying = false;
            }
        }

        protected void SetInstrument(AnywhenSampleInstrument instrument)
        {
            _playbackSettings.Instrument = instrument;
        }
    }
}