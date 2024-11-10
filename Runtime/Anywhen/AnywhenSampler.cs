using System;
using System.Collections;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnitySynth.Runtime.Synth;

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


        public AnywhenSampleInstrument Instrument => _instrument;
        private AnywhenSampleInstrument _instrument;
        public bool IsStopping => _isStopping;
        private bool _isStopping;
        public int CurrentNote => _currentNote;
        private int _currentNote;
        protected float Volume;
        private AnysongTrack _track;

        private bool _isLooping;

        private bool _isPlaying;
        private bool _scheduledPlay;
        public double ScheduledPlayTime => _scheduledPlayTime;
        public bool IsPlaying => _isPlaying;

        private double _scheduledPlayTime = -1;
        private double _scheduledStopTime;
        private AnywhenNoteClip _noteClip;
        ADSR _adsr = new();
        private bool _useEnvelope;

        private AnywhenSampleInstrument.LoopSettings _currentLoopSettings;
        private float _bufferFadeValue, _buffer2FadeValue;


        private double _samplePosBuffer1 /*, _samplePosBuffer2*/;
        private double _sampleStepFrac;
        private double _currentPitch;
        private float _pitch;
        private float _ampMod;

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


        public void NoteOn(int note, double playTime, double stopTime, float volume,
            AnywhenSampleInstrument newSettings, AnysongTrack track = null)
        {
            SetReady(false);
            _instrument = newSettings;
            if (_instrument == null)
            {
                //Debug.Log("settings was null");
                SetReady(true);
                return;
            }

            _currentNote = note;
            _scheduledPlayTime = playTime;
            _track = track;

            var noteClip = _instrument.GetNoteClip(note);
            if (noteClip != null)
            {
                Volume = volume;
                _isArmed = true;
                _audioSource.Stop();
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
            _instrument = null;
            IsReady = true;
            _isArmed = false;
        }

        public float GetDurationToEnd()
        {
            //todo, make this better
            return 0;
        }


        protected void StopScheduled(double absoluteTime)
        {
            _scheduledStopTime = absoluteTime;
        }

        public void SetPitch(float pitchValue)
        {
            _pitch = pitchValue;
        }

        protected void PlayScheduled(AnywhenNoteClip clip)
        {
            _samplePosBuffer1 = 0;
            _noteClip = clip;
            _scheduledPlay = true;
            _sampleStepFrac = clip.frequency / (float)AudioSettings.outputSampleRate;
            _currentPitch = 1;
            _pitch = 1;

            var currentEnvelopeSettings = new AnywhenSampleInstrument.EnvelopeSettings();

            if (_instrument != null)
            {
                if (_track != null)
                    currentEnvelopeSettings = _track.trackEnvelope;
                else
                    currentEnvelopeSettings = new AnywhenSampleInstrument.EnvelopeSettings(0, 1, 1, 0);
            }


            _useEnvelope = currentEnvelopeSettings.enabled;
            if (_useEnvelope)
            {
                SetEnvelope(currentEnvelopeSettings);
            }


            _currentLoopSettings = new AnywhenSampleInstrument.LoopSettings();
            if (_instrument != null)
            {
                _currentLoopSettings = _instrument.loopSettings;
            }

            if (clip.loopSettings.enabled)
            {
                _currentLoopSettings = clip.loopSettings;
            }


            _isLooping = _currentLoopSettings.enabled;
            _scheduledStopTime = -1;
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

            if (!_isPlaying && _scheduledPlay && AudioSettings.dspTime >= _scheduledPlayTime)
            {
                _isPlaying = true;
                _scheduledPlay = false;
                _isArmed = false;
                _adsr.SetGate(true);
            }

            if (!_isPlaying) return;

            if (_scheduledStopTime >= 0 && AudioSettings.dspTime > _scheduledStopTime)
            {
                _scheduledStopTime = -1;
                _adsr.SetGate(false);
                _isLooping = false;
            }

            DSP_WriteToBuffer(data);


            if (_isLooping)
            {
                DSP_HandleLooping();
            }

            if (_useEnvelope && _adsr.IsIdle)
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
                if (_useEnvelope)
                {
                    _ampMod *= _adsr.Process();
                }

                int sampleIndex1 = (int)_samplePosBuffer1;
                double f1 = _samplePosBuffer1 - sampleIndex1;
                var sourceSample1 = Mathf.Min((sampleIndex1), _noteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex1) + 1, _noteClip.clipSamples.Length - 1);
                double e1 = ((1 - f1) * _noteClip.clipSamples[sourceSample1]) +
                            (f1 * _noteClip.clipSamples[sourceSample2]);

                data[i] = ((float)(e1)) * _ampMod * _instrument.volume * Volume;

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;

                //_currentPitch = (Mathf.MoveTowards((float)_currentPitch, _pitch, 0.001f));


                i++;
            }

        }

        void DSP_HandleLooping()
        {
            if ((int)_samplePosBuffer1 >= _currentLoopSettings.loopStart)
            {
                _samplePosBuffer1 = (_currentLoopSettings.loopStart - _currentLoopSettings.loopLength) *
                                    (_sampleStepFrac * _currentPitch);
            }
        }


        public void SetReady(bool state)
        {
            IsReady = state;
            if (state)
            {
                _isArmed = false;
                _currentNote = -1000;
                _isPlaying = false;
            }
        }

        public void SetInstrument(AnywhenSampleInstrument instrument)
        {
            _instrument = instrument;
        }
    }
}