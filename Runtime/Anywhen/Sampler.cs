using Anywhen.SettingsObjects;
using Farmand.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class Sampler : MonoBehaviour
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


        public void Init(AnywhenMetronome.TickRate tickRate)
        {
            TryGetComponent(out _audioSource);
            IsReady = true;
            _tickRate = tickRate;
            _audioSource.playOnAwake = false;
        }


        private void Update()
        {
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

            var audioClip = _settings.GetAudioClip(note);

            if (audioClip == null)
            {
                Debug.LogWarning("failed to find AudioClip");
                return;
            }

            _queuedClip = audioClip;
            IsReady = false;
            _isArmed = true;
            _audioSource.clip = _queuedClip;
            _audioSource.volume = volume * _settings.volume;
            _audioSource.time = 0;
            _audioSource.outputAudioMixerGroup = mixerChannel;

            _audioSource.PlayScheduled(playTime);
        }


        public void NoteOff(double stopTime)
        {
            _isStopping = true;
            IsReady = false;
            if (stopTime != 0)
                stopTime -= AudioSettings.dspTime;


            Ticker.DelayedAction((float)stopTime, onDone: () =>
            {
                float startVolume = _audioSource.volume;
                float duration = _settings.stopDuration;
                Ticker.Tween(duration,
                    onUpdate: f => _audioSource.volume = Mathf.Lerp(startVolume, 0, f),
                    onDone: () =>
                    {
                        _audioSource.Stop();
                        Reset();
                    }
                );
            });
        }

        void Reset()
        {
            _isStopping = false;
            _settings = null;
            IsReady = true;
            _isArmed = false;
            //_overrideTick = false;
        }

        public float GetDurationToEnd()
        {
            if (_audioSource.clip == null) return 0;
            if (!_audioSource.isPlaying) return 0;
            return _audioSource.clip.length - _audioSource.time;
        }

        public void SetMixerGroup(AudioMixerGroup group)
        {
            _audioSource.outputAudioMixerGroup = group;
        }
    }
}