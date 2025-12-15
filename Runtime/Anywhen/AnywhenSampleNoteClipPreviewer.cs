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
        private Coroutine _fadeCoroutine;
        private Coroutine _envelopeCoroutine;

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
            NoteEvent n = new NoteEvent(0, 1);
            NoteOn(n);
        }

        public void PlayNoteClip(AnywhenNoteClip noteClip)
        {
            Init();
            _currentClip = noteClip;
            NoteEvent n = new NoteEvent(0, 1);
            PlaySampleDirectly(_currentClip, n);
        }

        public void PlayNoteClip(AnywhenNoteClip noteClip, NoteEvent noteEvent)
        {
            Init();
            _currentClip = noteClip;
            PlaySampleDirectly(_currentClip, noteEvent);
        }
        public void StopClip()
        {
            StopScheduled(1);
        }

        private void NoteOn(NoteEvent note)
        {
            if (_instrument == null) return;

            _currentClip = _instrument.GetNoteClip(note.notes[0]);
            if (_currentClip == null) return;

            PlaySampleDirectly(_currentClip, note);
        }

        private void PlaySampleDirectly(AnywhenNoteClip noteClip, NoteEvent noteEvent)
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
            // Cancel any ongoing fades/envelopes
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            if (_envelopeCoroutine != null)
            {
                StopCoroutine(_envelopeCoroutine);
                _envelopeCoroutine = null;
            }

            float targetVolume = Mathf.Max(0f, _volume * noteEvent.velocity);
            // start from zero and apply envelope
            _audioSource.volume = 0f;
            _audioSource.Play();

            // Calculate when the clip will end
            _scheduledEndTime = Time.time + tempClip.length;

            // Apply ADSR envelope and duration-based release
            _envelopeCoroutine = StartCoroutine(ApplyEnvelope(noteEvent.envelope, targetVolume, noteEvent.duration, tempClip.length));
        }

        private void StopScheduled(float fadeOutTime)
        {
            if (!_isInitialized || !_audioSource.isPlaying) return;

            // Cancel any ongoing fade
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            // Cancel any ongoing envelope
            if (_envelopeCoroutine != null)
            {
                StopCoroutine(_envelopeCoroutine);
                _envelopeCoroutine = null;
            }

            if (fadeOutTime <= 0f)
            {
                _audioSource.Stop();
                CleanupClip();
                _audioSource.volume = _volume; // restore default volume for next play
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOutTime));
        }

        private void Update()
        {
            // Auto-destroy temporary clips when they finish playing
            if (_isInitialized && _audioSource.clip != null && !_audioSource.isPlaying && Time.time >= _scheduledEndTime)
            {
                CleanupClip();
            }
        }

        private System.Collections.IEnumerator FadeOutAndStop(float duration)
        {
            float startVolume = _audioSource.volume;
            float t = 0f;
            while (t < duration && _audioSource != null && _audioSource.isPlaying)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                _audioSource.volume = startVolume * k;
                yield return null;
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.volume = _volume; // reset to default for next play
                CleanupClip();
            }

            _fadeCoroutine = null;
        }

        private void CleanupClip()
        {
            if (_audioSource != null && _audioSource.clip != null)
            {
                Destroy(_audioSource.clip);
                _audioSource.clip = null;
            }
        }

        private System.Collections.IEnumerator ApplyEnvelope(Anywhen.SettingsObjects.AnywhenSampleInstrument.EnvelopeSettings envelope,
            float targetVolume, float noteDuration, float clipLength)
        {
            // initialize envelope defaults if unset
            if (envelope.IsUnset())
            {
                envelope.Initialize();
            }

            float attack = Mathf.Max(0f, envelope.attack);
            float decay = Mathf.Max(0f, envelope.decay);
            float sustainLevel = Mathf.Clamp01(envelope.sustain);
            float release = Mathf.Max(0f, envelope.release);

            float startTime = Time.time;

            // Work out when to start release up-front so we can respect it during A/D too
            // Treat duration >= 0 as an explicit value; negatives mean "use clip length"
            float releaseStartTime;
            if (noteDuration >= 0f)
            {
                releaseStartTime = startTime + noteDuration;
            }
            else
            {
                // If no duration specified, start release so it ends at clip end
                releaseStartTime = startTime + Mathf.Max(0f, clipLength - release);
            }

            // Attack: 0 -> targetVolume
            if (attack > 0f)
            {
                float t = 0f;
                while (t < attack && _audioSource != null && _audioSource.isPlaying)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / attack);
                    _audioSource.volume = Mathf.Lerp(0f, targetVolume, k);
                    // If duration elapsed during attack, honor release immediately
                    if (Time.time >= releaseStartTime) break;
                    yield return null;
                }
            }
            else
            {
                if (_audioSource != null) _audioSource.volume = targetVolume;
            }

            // Decay: targetVolume -> sustainLevel * targetVolume
            float sustainVolume = targetVolume * sustainLevel;
            if (decay > 0f)
            {
                float t = 0f;
                float startVol = _audioSource.volume;
                while (t < decay && _audioSource != null && _audioSource.isPlaying)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / decay);
                    _audioSource.volume = Mathf.Lerp(startVol, sustainVolume, k);
                    // If duration elapsed during decay, honor release immediately
                    if (Time.time >= releaseStartTime) break;
                    yield return null;
                }
            }
            else
            {
                if (_audioSource != null) _audioSource.volume = sustainVolume;
            }

            // Sustain until release trigger

            while (_audioSource != null && _audioSource.isPlaying && Time.time < releaseStartTime)
            {
                // keep sustain volume while waiting
                _audioSource.volume = sustainVolume;
                yield return null;
            }

            // Release: volume -> 0
            if (_audioSource != null && _audioSource.isPlaying)
            {
                if (release > 0f)
                {
                    float t = 0f;
                    float startVol = _audioSource.volume;
                    while (t < release && _audioSource != null && _audioSource.isPlaying)
                    {
                        t += Time.deltaTime;
                        float k = Mathf.Clamp01(t / release);
                        _audioSource.volume = Mathf.Lerp(startVol, 0f, k);
                        yield return null;
                    }
                }

                // Stop and cleanup
                if (_audioSource != null)
                {
                    _audioSource.Stop();
                    _audioSource.volume = _volume; // reset for next play
                    CleanupClip();
                }
            }

            _envelopeCoroutine = null;
        }
    }
}