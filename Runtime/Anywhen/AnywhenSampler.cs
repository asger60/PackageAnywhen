using System;
using System.Collections;
using Anywhen.Attributes;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenSampler : MonoBehaviour
    {
        private AudioClip _queuedClip;

        public bool IsReady { get; private set; }
        private bool _isArmed;

        private AudioSource _audioSource;


        private AnywhenMetronome.TickRate _tickRate;
        public AnywhenInstrument Settings => _settings;
        private AnywhenInstrument _settings;
        public bool IsStopping => _isStopping;
        private bool _isStopping;
        public AnywhenMetronome.TickRate TickRate => _tickRate;
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
            _settings = newSettings;
            if (_settings == null)
            {
                Debug.LogWarning("settings was null");
                SetReady(true);
                return;
            }

            _currentNote = note;

            switch (_settings.clipType)
            {
                case AnywhenInstrument.ClipTypes.AudioClips:
                    var audioClip = _settings.GetAudioClip(note);

                    if (audioClip != null)
                    {
                        _queuedClip = audioClip;
                        _isArmed = true;
                        _audioSource.clip = _queuedClip;
                        _audioSource.Stop();
                        _audioSource.volume = volume * _settings.volume;
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
                    var noteClip = _settings.GetNoteClip(note);
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
                StartCoroutine(WaitAndStop((float)stopTime));
                //Ticker.DelayedAction((float)stopTime, onDone: () =>
                //{
                //    float startVolume = _audioSource.volume;
                //    float duration = _settings.stopDuration;
                //    Ticker.Tween(duration,
                //        onUpdate: f => _audioSource.volume = Mathf.Lerp(startVolume, 0, f),
                //        onDone: () =>
                //        {
                //            _audioSource.Stop();
                //            //Reset();
                //        }
                //    );
                //});
            }

            //IsReady = false;
            //if (stopTime != 0)
            //    stopTime -= AudioSettings.dspTime;
        }

        IEnumerator WaitAndStop(float stopTime)
        {
            yield return new WaitForSeconds(stopTime);
            float startVolume = _audioSource.volume;
            float duration = _settings.stopDuration;
            float f = 0;
            while (f < duration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, 0, f/duration);
                f += Time.deltaTime;
                yield return 0;
            }
            _audioSource.Stop();
        }

        void Reset()
        {
            _isStopping = false;
            _settings = null;
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
        private bool _alternateBuffer;
        private float _buffer1Amp, _buffer2Amp;
        private float _bufferFadeValue, _buffer2FadeValue;
        private double _bufferCrossFadeStepValue = 0.0025f;

        protected void StopScheduled(double absoluteTime)
        {
            _scheduledStopTime = absoluteTime;
        }

        public void SetPitch(float pitchValue)
        {
            _pitch = pitchValue;
        }

        protected void PlayScheduled(double absolutePlayTime, AnywhenNoteClip clip)
        {
            _alternateBuffer = false;
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

            if (_settings != null)
                currentEnvelopeSettings = _settings.envelopeSettings;

            if (clip.envelopeSettings.enabled)
            {
                currentEnvelopeSettings = clip.envelopeSettings;
            }

            _useEnvelope = currentEnvelopeSettings.enabled;
            if (_useEnvelope)
                SetEnvelope(currentEnvelopeSettings);


            _currentLoopSettings = new AnywhenInstrument.LoopSettings();
            if (_settings != null)
            {
                _currentLoopSettings = _settings.loopSettings;
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
            _adsr.setAttackRate(envelopeSettings.attack * AudioSettings.outputSampleRate);
            _adsr.setDecayRate(envelopeSettings.decay * AudioSettings.outputSampleRate);
            _adsr.setReleaseRate(envelopeSettings.release * AudioSettings.outputSampleRate);
            _adsr.setSustainLevel(envelopeSettings.sustain);
            _adsr.reset();
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

                //data[i] = (float)e * _ampMod;


                int sampleIndex2 = (int)_samplePosBuffer2;
                double f2 = _samplePosBuffer2 - sampleIndex2;
                var sourceSample3 = Mathf.Min((sampleIndex2), _noteClip.clipSamples.Length - 1);
                var sourceSample4 = Mathf.Min((sampleIndex2) + 1, _noteClip.clipSamples.Length - 1);

                double e2 = ((1 - f2) * _noteClip.clipSamples[sourceSample3]) +
                            (f2 * _noteClip.clipSamples[sourceSample4]);


                data[i] = ((float)(e1 * _buffer1Amp) + (float)(e2 * _buffer2Amp)) * _ampMod;


                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;
                _samplePosBuffer2 += (_sampleStepFrac * _currentPitch) / 2f;


                _bufferFadeValue += (float)_bufferCrossFadeStepValue;

                if (_alternateBuffer)
                {
                    _buffer1Amp = Lin(1 - _bufferFadeValue);
                    _buffer2Amp = Lin(_bufferFadeValue);
                }
                else
                {
                    _buffer1Amp = Lin(_bufferFadeValue);
                    _buffer2Amp = Lin(1 - _bufferFadeValue);
                }


                _buffer1Amp = Mathf.Clamp01(_buffer1Amp);
                _buffer2Amp = Mathf.Clamp01(_buffer2Amp);
                _currentPitch = (Mathf.MoveTowards((float)_currentPitch, _pitch, 0.001f));

                i++;
            }


            if (_isLooping)
            {
                if (!_alternateBuffer && (int)_samplePosBuffer1 > _currentLoopSettings.loopStart)
                {
                    _samplePosBuffer2 = (_currentLoopSettings.loopStart - _currentLoopSettings.loopLength) *
                                        (_sampleStepFrac * _currentPitch);
                    _alternateBuffer = true;
                    _buffer2Amp = 0;
                    _bufferFadeValue = 0;
                }
                else if (_alternateBuffer && (int)_samplePosBuffer2 > _currentLoopSettings.loopStart)
                {
                    _samplePosBuffer1 = (_currentLoopSettings.loopStart - _currentLoopSettings.loopLength) *
                                        (_sampleStepFrac * _currentPitch);
                    _alternateBuffer = false;
                    _buffer1Amp = 0;
                    _bufferFadeValue = 0;
                }
            }

            if (_useEnvelope && _adsr.IsIdle)
            {
                _isPlaying = false;
                IsReady = true;
            }

            if (_samplePosBuffer1 >= _noteClip.clipSamples.Length)
            {
                _adsr.SetGate(false);
                _isPlaying = false;
                IsReady = true;
            }
        }


        float Log(float x)
        {
            //return 1 - Mathf.Exp(-Mathf.Log((1.0f + 0.3f) / 0.3f) / x);
            return Mathf.Clamp01(1 - Mathf.Pow(1 - x, 5));
        }

        float Lin(float x)
        {
            //return 1 - Mathf.Exp(-Mathf.Log((1.0f + 0.3f) / 0.3f) / x);
            return Mathf.Clamp01(x);
        }


        public void SetReady(bool state)
        {
            IsReady = state;
        }
    }
}