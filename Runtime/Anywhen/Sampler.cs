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
            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);


            TryGetComponent(out _audioSource);
            IsReady = true;
            _tickRate = tickRate;
            _audioSource.playOnAwake = true;
            _audioSource.clip = myClip;
        }


        private void Update()
        {
            //if (_isArmed && !_audioSource.isPlaying)
            //{
            //    _isArmed = false;
            //    IsReady = true;
            //}
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
            //_audioSource.clip = _queuedClip;
            _audioSource.Play();
            _audioSource.volume = volume * _settings.volume;
            _audioSource.time = 0;
            _audioSource.outputAudioMixerGroup = mixerChannel;

//            _audioSource.PlayScheduled(playTime);
            PlayScheduled(playTime, newSettings.noteClip);
        }


        public void NoteOff(double stopTime)
        {
            _isStopping = true;
            IsReady = false;
            //if (stopTime != 0)
            //    stopTime -= AudioSettings.dspTime;


            StopScheduled(stopTime);
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

        void Reset()
        {
            _isStopping = false;
            _settings = null;
            IsReady = true;
            _isArmed = false;
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


        private float[] _clipSamples;
        private int _sampleRate;
        public float loopStart;

        public int loopLength;

        //public bool noteDown;
        private bool _isPlaying;
        private int _sampleIndex = 0;
        private bool _scheduledPlay;
        private double _scheduledPlayTime = -1;
        private double _scheduledStopTime;
        private bool _noteDown;
        
        void StopScheduled(double absoluteTime)
        {
            print("stop " + absoluteTime);
            _scheduledStopTime = absoluteTime;
        }
        
        void PlayScheduled(double absolutePlayTime, AnywhenNoteClip clip)
        {
            _sampleRate = clip.audioClip.frequency;
            _clipSamples = new float[clip.audioClip.samples];

            if (!clip.audioClip.GetData(_clipSamples, 0))
            {
                Debug.Log("Uh oh, sourceClip1 was unreadable!");
            }

            loopStart = clip.loopStart;
            loopLength = clip.loopLength;
            Debug.Log("schedule play " + absolutePlayTime, transform);
            _scheduledPlay = true;
            _scheduledPlayTime = absolutePlayTime;
            _isArmed = true;
            IsReady = false;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_clipSamples == null)
            {
                return;
            }

            if (!_isPlaying && _scheduledPlay && _scheduledPlayTime > 0 && AudioSettings.dspTime >= _scheduledPlayTime)
            {
                _isPlaying = true;
                _sampleIndex = 0;
                _scheduledPlay = false;
                _isArmed = false;
                _noteDown = true;
            }

            if (!_isPlaying) return;

            if (_scheduledStopTime > 0 && AudioSettings.dspTime > _scheduledStopTime)
            {
                _noteDown = false;
                _scheduledStopTime = -1;
            }
            
            //if (noteDown && !_isPlaying)
            //{
            //    _isPlaying = true;
            //    _thisSample = 0;
            //}


            for (int i = 0; i < data.Length; i++)
            {
                var sourceSample1 = Mathf.Max(Mathf.Min(_sampleIndex + (i / 2), _clipSamples.Length - 1), 0);
                data[i] = _clipSamples[sourceSample1];
            }

            if (_isPlaying && _noteDown && _sampleIndex > loopStart)
            {
                _sampleIndex -= loopLength;
            }
            else
            {
                _sampleIndex += data.Length / 2;
            }

            if (_sampleIndex >= _clipSamples.Length)
            {
                _isPlaying = false;
                IsReady = true;
            }
        }
    }
}