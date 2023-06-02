using System;
using Anywhen.SettingsObjects;
using Farmand.Utilities;
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

        public void Init(AnywhenMetronome.TickRate tickRate)
        {
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            TryGetComponent(out _audioSource);
            IsReady = true;
            _tickRate = tickRate;
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
            _adsr = new ADSR();
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


        public void NoteOn(int note, double playTime, float volume, AnywhenInstrument newSettings,
            AudioMixerGroup mixerChannel = null)
        {
            _settings = newSettings;
            if (_settings == null)
            {
                Debug.LogWarning("settings was null");
                return;
            }

            switch (_settings.clipType)
            {
                case AnywhenInstrument.ClipTypes.AudioClips:
                    var audioClip = _settings.GetAudioClip(note);

                    if (audioClip != null)
                    {
                        _queuedClip = audioClip;
                        IsReady = false;
                        _isArmed = true;
                        _audioSource.clip = _queuedClip;
                        _audioSource.Play();
                        _audioSource.volume = volume * _settings.volume;
                        _audioSource.time = 0;
                        _audioSource.outputAudioMixerGroup = mixerChannel;
                        _playingNoteClip = false;
                        _audioSource.PlayScheduled(playTime);
                    }
                    else
                    {
                        Debug.LogWarning("failed to find AudioClip");
                    }

                    break;
                case AnywhenInstrument.ClipTypes.NoteClips:
                    var noteClip = _settings.GetNoteClip(note);
                    if (noteClip != null)
                    {
                        IsReady = false;
                        _isArmed = true;
                        _playingNoteClip = true;
                        PlayScheduled(playTime, noteClip);
                    }
                    else
                    {
                        Debug.LogWarning("failed to find NoteClip");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void NoteOff(double stopTime)
        {
            _isStopping = true;
            if (_playingNoteClip)
            {
                StopScheduled(stopTime);
            }
            else
            {
                Ticker.DelayedAction((float)stopTime, onDone: () =>
                {
                    float startVolume = _audioSource.volume;
                    float duration = _settings.stopDuration;
                    Ticker.Tween(duration,
                        onUpdate: f => _audioSource.volume = Mathf.Lerp(startVolume, 0, f),
                        onDone: () =>
                        {
                            _audioSource.Stop();
                            //Reset();
                        }
                    );
                });
            }

            //IsReady = false;
            //if (stopTime != 0)
            //    stopTime -= AudioSettings.dspTime;
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

        protected void StopScheduled(double absoluteTime)
        {
            _scheduledStopTime = absoluteTime;
        }




        protected void PlayScheduled(double absolutePlayTime, AnywhenNoteClip clip)
        {
            _audioSource.Play();
            _samplePos = 0;
            _noteClip = clip;

            _scheduledPlay = true;
            _scheduledPlayTime = absolutePlayTime;
            _sampleStepFrac = clip.frequency / (float)AudioSettings.outputSampleRate;

            var currentEnvelopeSettings = new AnywhenInstrument.EnvelopeSettings();

            if (_settings != null)
                currentEnvelopeSettings = _settings.envelopeSettings;

            if (clip.envelopeSettings.enabled)
                currentEnvelopeSettings = clip.envelopeSettings;

            _useEnvelope = currentEnvelopeSettings.enabled;
            SetEnvelope(currentEnvelopeSettings);


             _currentLoopSettings = new AnywhenInstrument.LoopSettings();
            if (_settings != null)
            {
                _currentLoopSettings = _settings.loopSettings;
            }

            if (clip.loopSettings.enabled)
                _currentLoopSettings = clip.loopSettings;


            _isLooping = _currentLoopSettings.enabled;
            SetLoop(_currentLoopSettings);


            _scheduledStopTime = -1;
        }

        void SetEnvelope(AnywhenInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.setAttackRate(envelopeSettings.attack);
            _adsr.setDecayRate(envelopeSettings.decay);
            _adsr.setSustainLevel(envelopeSettings.sustain);
            _adsr.setReleaseRate(envelopeSettings.release);
            _adsr.reset();
        }

        void SetLoop(AnywhenInstrument.LoopSettings loopSettings)
        {
            _currentLoopSettings = loopSettings;
        }

        private double _samplePos;
        private double _sampleStepFrac;
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

            if (_scheduledStopTime > 0 && AudioSettings.dspTime > _scheduledStopTime)
            {
                _scheduledStopTime = -1;
                _adsr.SetGate(false);
                _isLooping = false;
            }


            int i = 0;
            _ampMod = 1;
            if (_useEnvelope)
                _ampMod *= _adsr.Process();

            while (i < data.Length)
            {
                int sampleIndex = (int)_samplePos;
                double f = _samplePos - sampleIndex;
                var sourceSample1 = Mathf.Min((sampleIndex), _noteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex) + 1, _noteClip.clipSamples.Length - 1);

                double e = ((1 - f) * _noteClip.clipSamples[sourceSample1]) +
                           (f * _noteClip.clipSamples[sourceSample2]);

                data[i] = (float)e * _ampMod;

                _samplePos += _sampleStepFrac / 2f;
                i++;
            }

            if (_isLooping && (int)_samplePos > _currentLoopSettings.loopStart)
            {
                _samplePos -= _currentLoopSettings.loopLength * _sampleStepFrac;
            }

            if (_useEnvelope && _adsr.IsIdle)
            {
                _isPlaying = false;
                IsReady = true;
            }

            if (_samplePos >= _noteClip.clipSamples.Length)
            {
                _adsr.SetGate(false);
                _isPlaying = false;
                IsReady = true;
            }
        }

        public void SetReady(bool state)
        {
            IsReady = state;
        }
    }
}