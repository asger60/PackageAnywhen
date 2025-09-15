using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenPlayerBase : MonoBehaviour
    {
        private bool _isMuted;
        protected bool IsRunning;
        private int _currentSectionIndex = 0;

        public int CurrentSectionIndex
        {
            get => _currentSectionIndex;
            set => _currentSectionIndex = value;
        }

        private float _playerVolume = 1;
        protected int _triggerStepIndex = -1;
        internal int CurrentBar;
        internal AnysongObject CurrentSong;
        [SerializeField] protected bool sectionsAutoAdvance = true;

        [FormerlySerializedAs("anywhenSamplerPrefab")] [SerializeField]
        AnywhenVoice anywhenVoicePrefab;

        [SerializeField] protected AudioMixerGroup outputMixerGroup;


        bool _resetOnNextBar;
        private int _bufferSize;
        private int _numBuffers;

        [Serializable]
        public class PlayerVoices
        {
            public AnywhenInstrument instrument;
            [FormerlySerializedAs("trackType")] public AnysongTrack track;

            public AnywhenVoice[] voices;

            public PlayerVoices(AnywhenInstrument instrument, AnysongTrack type, AnywhenVoice[] voices)
            {
                this.instrument = instrument;
                this.track = type;
                this.voices = voices;
            }

            public AnywhenVoice GetVoice()
            {
                foreach (var voiceVoice in voices)
                {
                    if (voiceVoice.HasScheduledPlay) continue;
                    if (voiceVoice.IsReady)
                    {
                        return voiceVoice;
                    }
                }

                float maxTime = float.MaxValue;
                AnywhenVoice bestVoice = null;

                foreach (var voiceVoice in voices)
                {
                    if (voiceVoice.HasScheduledPlay) continue;

                    var thisTime = voiceVoice.GetDurationToEnd();
                    if (thisTime < maxTime)
                    {
                        maxTime = thisTime;
                        bestVoice = voiceVoice;
                    }
                }

                return bestVoice ?? voices[0];
            }
        }

        private List<PlayerVoices> _voicesList = new();

        protected virtual void Start()
        {
            AudioSettings.GetDSPBufferSize(out _bufferSize, out _numBuffers);
            print("buffersize " + _bufferSize);

            AudioClip myClip = AudioClip.Create("MySound", 2, 1, 44100, false);
            AudioSource source = GetComponent<AudioSource>();
            source.playOnAwake = true;
            source.clip = myClip;
            source.Play();
        }

        protected void SetupVoices(List<AnysongTrack> tracks)
        {
            _voicesList.Clear();
            //foreach (var anywhenVoice in transform.gameObject.GetComponentsInChildren<AnywhenVoice>())
            //{
            //    DestroyImmediate(anywhenVoice.gameObject);
            //}

            foreach (var songTrack in tracks)
            {
                List<AnywhenVoice> voices = new();

                if (songTrack.monophonic)
                {
                    //var newSampler = (GameObject)(Instantiate(Resources.Load("AnywhenSampler"), transform));
                    //var newVoice = newSampler.GetComponent<AnywhenVoice>();
                    //var source = newSampler.GetComponent<AudioSource>();
                    //if (outputMixerGroup)
                    //    source.outputAudioMixerGroup = outputMixerGroup;
                    voices.Add(new AnywhenVoice());
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        //var newSampler = (GameObject)(Instantiate(Resources.Load("AnywhenSampler"), transform));
                        //var newVoice = newSampler.GetComponent<AnywhenVoice>();
                        //var source = newVoice.GetComponent<AudioSource>();
                        //if (outputMixerGroup)
                        //    source.outputAudioMixerGroup = outputMixerGroup;
                        voices.Add(new AnywhenVoice());
                    }
                }

                foreach (var anywhenSampler in voices)
                {
                    anywhenSampler.Init(AudioSettings.outputSampleRate);
                }

                _voicesList.Add(new PlayerVoices(songTrack.instrument, songTrack, voices.ToArray()));
            }
        }

        private void OnTick16()
        {
            if (!IsRunning) return;
            _currentSectionIndex = Mathf.Min(_currentSectionIndex, CurrentSong.Sections.Count - 1);
            TriggerStep(-1, AnywhenMetronome.TickRate.Sub16);
        }

        private void TriggerStep(int stepIndex, AnywhenMetronome.TickRate tickRate)
        {
            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < CurrentSong.Sections.Count; sectionIndex++)
                {
                    var section = CurrentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];


                    var track = CurrentSong.Tracks[trackIndex];
                    var pattern = sectionTrack.GetPlayingPattern();


                    var step = pattern.GetCurrentStep();

                    if (stepIndex >= 0)
                    {
                        step = pattern.GetStep(stepIndex);
                    }
                    else if (_triggerStepIndex >= 0)
                    {
                        step = pattern.GetStep(_triggerStepIndex);
                    }

                    if (tickRate != AnywhenMetronome.TickRate.None)
                        pattern.Advance();


                    if (sectionIndex == _currentSectionIndex && (step.noteOn || step.noteOff))
                    {
                        float thisIntensity = Mathf.Clamp01(track.intensityMappingCurve.Evaluate(1));
                        float thisRnd = Random.Range(0, 1f);
                        if (thisRnd < step.chance && step.mixWeight < thisIntensity && !_isMuted)
                        {
                            TriggerNotePlayback(tickRate, trackIndex, step);
                        }
                    }
                }
            }


            if (_triggerStepIndex >= 0)
            {
                _triggerStepIndex = -1;
            }
        }

        protected virtual void TriggerNotePlayback(AnywhenMetronome.TickRate tickRate, int trackIndex, AnyPatternStep step)
        {
            var songTrack = CurrentSong.Tracks[trackIndex];

            var thisStep = step.Clone();
            //thisStep.rootNote += rootNoteMod;
            thisStep.velocity *= _playerVolume;

            var sampleInstrument = songTrack.instrument as AnywhenSampleInstrument;
            if (!sampleInstrument) return;


            var s = thisStep.GetEvent(0);

            foreach (var note in s.notes)
            {
                var voice = GetVoice(songTrack);
                if (voice != null)
                {
                    var playTime = AnywhenMetronome.Instance.GetScheduledPlaytime(tickRate) +
                                   (AnywhenMetronome.Instance.GetLength(tickRate) * thisStep.offset);
                    var volume = thisStep.velocity * songTrack.volume;
                    var envelope = songTrack.trackEnvelope;
                    voice.NoteOn(note, playTime, playTime + thisStep.duration, volume, sampleInstrument, envelope);
                }
            }
        }


        private void OnDisable()
        {
            ReleaseFromMetronome();
        }

        private void OnDestroy()
        {
            ReleaseFromMetronome();
        }

        protected virtual void ReleaseFromMetronome()
        {
            IsRunning = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            AnywhenRuntime.Metronome.OnNextBar -= OnBar;
        }

        protected virtual void AttachToMetronome()
        {
            if (IsRunning) return;
            IsRunning = true;
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.Metronome.OnNextBar += OnBar;
            CurrentSong.Reset();
        }

        protected virtual void OnBar()
        {
            if (!IsRunning) return;
            if (_resetOnNextBar)
            {
                CurrentBar = 0;
                _resetOnNextBar = false;
            }
            else
            {
                CurrentBar++;
            }

            if (AnywhenMetronome.Instance.CurrentBar % 4 == 0 && CurrentBar % 4 != 0)
            {
                CurrentBar = 0;
            }

            _currentSectionIndex = Mathf.Min(_currentSectionIndex, CurrentSong.Sections.Count - 1);

            var thisSection = CurrentSong.Sections[_currentSectionIndex];
            int progress = (int)Mathf.Repeat(CurrentBar, thisSection.sectionLength);


            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                var track = CurrentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                track.AdvancePlayingPattern();
            }


            if (progress == 0)
            {
                NextSection();
            }
        }


        public virtual void Play()
        {
            if (CurrentSong == null)
            {
                return;
            }


            if (CurrentSong)
            {
                CurrentSong.Reset();
                _currentSectionIndex = 0;
            }

            SetupVoices(CurrentSong.Tracks);

            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < CurrentSong.Sections.Count; sectionIndex++)
                {
                    var section = CurrentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[0];
                    sectionTrack.Reset();
                }
            }


            CurrentBar = 0;
            AttachToMetronome();
        }

        public virtual void Stop()
        {
            ReleaseFromMetronome();
        }

        void NextSection()
        {
            CurrentSong.Reset();
            CurrentBar = 0;

            _currentSectionIndex++;
            _currentSectionIndex = (int)Mathf.Repeat(_currentSectionIndex, CurrentSong.Sections.Count);
            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                var track = CurrentSong.Sections[_currentSectionIndex].tracks[trackIndex];
                track.Reset();
            }
        }

        protected virtual AnywhenVoice GetVoice(AnysongTrack track)
        {
            foreach (var voice in _voicesList)
            {
                if (IsDrums(track.trackType) && voice.instrument != track.instrument) continue;
                if (track == voice.track)
                    return voice.GetVoice();
            }

            print("no voice found for track " + track.trackType);
            return null;
        }

        bool IsDrums(AnysongTrack.AnyTrackTypes trackType)
        {
            return trackType is AnysongTrack.AnyTrackTypes.Clap
                or AnysongTrack.AnyTrackTypes.Snare
                or AnysongTrack.AnyTrackTypes.Hihat
                or AnysongTrack.AnyTrackTypes.Kick
                or AnysongTrack.AnyTrackTypes.Tick
                or AnysongTrack.AnyTrackTypes.Tom;
        }


        public void SetVolume(float value)
        {
            _playerVolume = value;
        }


        public void ResetOnNextBar()
        {
            _resetOnNextBar = true;

            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < CurrentSong.Sections.Count; sectionIndex++)
                {
                    var section = CurrentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    sectionTrack.ResetOnNextBar();
                    section.Reset();
                }
            }
        }

        public void Reset()
        {
            if (!CurrentSong) return;
            for (int trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                for (var sectionIndex = 0; sectionIndex < CurrentSong.Sections.Count; sectionIndex++)
                {
                    var section = CurrentSong.Sections[sectionIndex];
                    var sectionTrack = section.tracks[trackIndex];
                    sectionTrack.Reset();
                    section.Reset();
                }
            }
        }

        public void SetIsMuted(bool state)
        {
            _isMuted = state;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!IsRunning) return;
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0f;
            }


            // Mix in each voice group
            foreach (var voice in _voicesList)
            {
                foreach (var anywhenVoice in voice.voices)
                {
                    var voiceDSP = anywhenVoice.UpdateDSP(data.Length, _numBuffers);
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] += voiceDSP[i];
                    }
                }
            }
        }
    }
}