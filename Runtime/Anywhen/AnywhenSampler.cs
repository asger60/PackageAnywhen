using System;
using System.Collections;
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
        private AudioClip _queuedClip;

        public bool IsReady { get; private set; }
        public bool IsArmed => _isArmed;
        private bool _isArmed;

        private AudioSource _audioSource;


        private AnywhenMetronome.TickRate _tickRate;
        public AnywhenInstrument Instrument => _instrument;
        private AnywhenInstrument _instrument;
        public bool IsStopping => _isStopping;
        private bool _isStopping;
        private bool _playingNoteClip;
        public int CurrentNote => _currentNote;
        private int _currentNote;
        

        public void Init(AnywhenMetronome.TickRate tickRate)
        {
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            TryGetComponent(out _audioSource);
            IsReady = true;
            _tickRate = tickRate;
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
            _adsr = new ADSR();
            _audioSource.Play();
        }


        private void Update()
        {
            if (_playingNoteClip) return;
            if (_isArmed && !_audioSource.isPlaying)
            {
                _isArmed = false;
                IsReady = true;
            }
        }


        public void NoteOn(int note, double playTime, double stopTime, float volume, AnywhenInstrument newSettings,
            AudioMixerGroup mixerChannel = null)
        {
            SetReady(false);
            _instrument = newSettings;
            if (_instrument == null)
            {
                Debug.LogWarning("settings was null");
                SetReady(true);
                return;
            }

            _currentNote = note;

            switch (_instrument.clipType)
            {
                case AnywhenInstrument.ClipTypes.AudioClips:
                    var audioClip = _instrument.GetAudioClip(note);

                    if (audioClip != null)
                    {
                        _queuedClip = audioClip;
                        _isArmed = true;
                        _audioSource.clip = _queuedClip;
                        _audioSource.Stop();
                        _audioSource.volume = volume * _instrument.volume;
                        _audioSource.time = 0;
                        _audioSource.outputAudioMixerGroup = mixerChannel;
                        _playingNoteClip = false;
                        _audioSource.PlayScheduled(playTime);
                    }
                    else
                    {
                        Debug.LogWarning("failed to find AudioClip");
                        SetReady(true);
                    }

                    break;
                case AnywhenInstrument.ClipTypes.NoteClips:
                    var noteClip = _instrument.GetNoteClip(note);
                    if (noteClip != null)
                    {
                        _isArmed = true;
                        _playingNoteClip = true;
                        _audioSource.Stop();
                        PlayScheduled(playTime, noteClip);
                        _hasScheduledStop = false;
                        if (stopTime > 0)
                        {
                            StopScheduled(stopTime);
                            _hasScheduledStop = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("failed to find NoteClip");
                        SetReady(true);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool _hasScheduledStop;

        public void NoteOff(double stopTime)
        {
            if (_hasScheduledStop) return;
            _isStopping = true;
            if (_playingNoteClip)
            {
                StopScheduled(stopTime);
            }
            else
            {
                _audioSource.SetScheduledEndTime(stopTime);
                StartCoroutine(FadeOut((float)stopTime));
            }
        }

        IEnumerator FadeOut(float fadeTime)
        {
            float startVolume = _audioSource.volume;
            float t = 0;
            while (t / fadeTime < 1)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                t += Time.deltaTime;
                yield return 0;
            }

            _audioSource.Stop();
        }

        IEnumerator WaitAndStop(float stopTime)
        {
            yield return new WaitForSeconds(stopTime);
            float startVolume = _audioSource.volume;
            float duration = _instrument.stopDuration;
            float f = 0;
            while (f < duration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, 0, f / duration);
                f += Time.deltaTime;
                yield return 0;
            }

            _audioSource.Stop();
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
            if (_playingNoteClip)
            {
                //todo, make this better
                return 0;
            }

            if (_audioSource.clip == null) return 0;
            if (!_audioSource.isPlaying) return 0;
            return _audioSource.clip.length - _audioSource.time;
        }

        public void SetMixerGroup(AudioMixerGroup group)
        {
            _audioSource.outputAudioMixerGroup = group;
        }


        private bool _isLooping;

        private bool _isPlaying;
        private bool _scheduledPlay;
        private double _scheduledPlayTime = -1;
        private double _scheduledStopTime;
        private AnywhenNoteClip _noteClip;
        ADSR _adsr = new ADSR();
        private bool _useEnvelope;

        private AnywhenInstrument.LoopSettings _currentLoopSettings;

        //private bool _alternateBuffer;
        private float _buffer1Amp, _buffer2Amp;
        private float _bufferFadeValue, _buffer2FadeValue;
        private double _bufferCrossFadeStepValue = 0.0025f;

        protected void StopScheduled(double absoluteTime)
        {
            _scheduledStopTime = absoluteTime;
            _currentNote = -1000;
        }

        public void SetPitch(float pitchValue)
        {
            _pitch = pitchValue;
        }

        protected void PlayScheduled(double absolutePlayTime, AnywhenNoteClip clip)
        {
            //_alternateBuffer = false;
            _buffer1Amp = 1;
            _buffer2Amp = 0;
            _samplePosBuffer1 = 0;
            _samplePosBuffer2 = 0;


            _noteClip = clip;

            _scheduledPlay = true;
            _scheduledPlayTime = absolutePlayTime;
            _sampleStepFrac = clip.frequency / (float)AudioSettings.outputSampleRate;
            _currentPitch = 1;
            _pitch = 1;

            var currentEnvelopeSettings = new AnywhenInstrument.EnvelopeSettings();

            if (_instrument != null)
                currentEnvelopeSettings = _instrument.envelopeSettings;

            if (clip.envelopeSettings.enabled)
            {
                currentEnvelopeSettings = clip.envelopeSettings;
            }

            _useEnvelope = currentEnvelopeSettings.enabled;
            if (_useEnvelope)
                SetEnvelope(currentEnvelopeSettings);


            _currentLoopSettings = new AnywhenInstrument.LoopSettings();
            if (_instrument != null)
            {
                _currentLoopSettings = _instrument.loopSettings;
            }

            if (clip.loopSettings.enabled)
            {
                _currentLoopSettings = clip.loopSettings;
            }

            _bufferCrossFadeStepValue = 0.2f / _currentLoopSettings.crossFadeDuration;


            _isLooping = _currentLoopSettings.enabled;


            _scheduledStopTime = -1;
        }

        void SetEnvelope(AnywhenInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * AudioSettings.outputSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * AudioSettings.outputSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * AudioSettings.outputSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }


        private double _samplePosBuffer1, _samplePosBuffer2;
        private double _sampleStepFrac;
        private double _currentPitch;
        private float _pitch;
        private float _ampMod;

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

                data[i] = ((float)(e1)) * _ampMod * _instrument.volume;

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;

                _currentPitch = (Mathf.MoveTowards((float)_currentPitch, _pitch, 0.001f));
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
    }
}