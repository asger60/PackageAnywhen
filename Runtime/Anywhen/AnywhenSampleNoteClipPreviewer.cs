using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenSampleNoteClipPreviewer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AnywhenSampleInstrument _instrument;
        private AnywhenNoteClip _currentClip;
        private bool _isInitialized;
        private float _scheduledEndTime;
        private float _volume = 1f;
        
        private void Init()
        {
            if (_isInitialized) return;
            
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _isInitialized = true;
        }
        
        public void SetInstrument(AnywhenSampleInstrument instrument)
        {
            Init();
            _instrument = instrument;
            _volume = instrument?.volume ?? 1f;
        }
        
        public void PlayClip(AnywhenSampleInstrument instrument)
        {
            Init();
            SetInstrument(instrument);
            NoteOn(0, 0, -1, 1);
        }
        
        public void PlayNoteClip(AnywhenNoteClip noteClip)
        {
            Init();
            _currentClip = noteClip;
            PlaySampleDirectly(noteClip);
        }
        
        public void StopClip()
        {
            StopScheduled(1);
        }
        
        private void NoteOn(int note, float startTime, int duration, float velocity)
        {
            if (_instrument == null) return;
            
            _currentClip = _instrument.GetNoteClip(note);
            if (_currentClip == null) return;
            
            PlaySampleDirectly(_currentClip, velocity);
        }
        
        private void PlaySampleDirectly(AnywhenNoteClip noteClip, float velocity = 1f)
        {
            if (noteClip == null || noteClip.clipSamples == null || noteClip.clipSamples.Length == 0) return;
            
            // Create a temporary AudioClip from the samples
            AudioClip tempClip = AudioClip.Create(
                "TempPreviewClip", 
                noteClip.clipSamples.Length / noteClip.channels, 
                noteClip.channels, 
                noteClip.frequency, 
                false
            );
            
            // Set the data
            tempClip.SetData(noteClip.clipSamples, 0);
            
            // Play the clip
            _audioSource.clip = tempClip;
            _audioSource.volume = _volume * velocity;
            _audioSource.Play();
            
            // Calculate when the clip will end
            _scheduledEndTime = Time.time + tempClip.length;
        }
        
        private void StopScheduled(float fadeOutTime)
        {
            if (!_isInitialized || !_audioSource.isPlaying) return;
            
            // Simple immediate stop for now
            _audioSource.Stop();
            
            // In a more advanced implementation, you could add a fade-out effect here
        }
        
        private void Update()
        {
            // Auto-destroy temporary clips when they finish playing
            if (_isInitialized && _audioSource.clip != null && !_audioSource.isPlaying && Time.time >= _scheduledEndTime)
            {
                Destroy(_audioSource.clip);
                _audioSource.clip = null;
            }
        }
    }
}