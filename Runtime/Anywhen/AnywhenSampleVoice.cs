using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;


namespace Anywhen
{
    public class AnywhenSampleVoice : AnywhenVoiceBase
    {
        public int CurrentNote => CurrentPlaybackSettings.Note;

        public double ScheduledPlayTime => CurrentPlaybackSettings.PlayTime;


        private ADSR _adsr = new();

        private float _bufferFadeValue, _buffer2FadeValue;


        private double _samplePosBuffer1;
        private double _sampleStepFrac;

        private double _currentPitch;

        private float _ampMod;

        public double SampleposBuffer1 => _samplePosBuffer1;

        AnywhenNoteClip _currentNoteClip, _nextNoteClip;
        SynthControlLFO _pitchLFO;

        private float _currentSampleRate;
        AnysongTrack _currentTrack;
        private SynthSettingsObjectLFO _pitchSettings;

        public override void Init(int currentSampleRate, AnywhenInstrument instrumentSettings, AnysongTrack trackSettings)
        {
            _currentTrack = trackSettings;
            _thisInstrument = instrumentSettings as AnywhenSampleInstrument;
            IsReady = true;
            _adsr = new ADSR();
            _pitchLFO = new SynthControlLFO();
            _currentSampleRate = currentSampleRate;
        }


        AnywhenSampleInstrument _thisInstrument;


        public override void NoteOn(PlaybackSettings playbackSettings)
        {
            if (AudioSettings.dspTime > playbackSettings.PlayTime)
            {
                return;
            }


            SetPitchLFO(_currentTrack.pitchLFOSettings);
            SetEnvelope(_currentTrack.trackEnvelope);

            //PlayScheduled(playbackSettings, _thisInstrument.GetNoteClip(playbackSettings.Note));
            NextPlaybackSettings = playbackSettings;
            _nextNoteClip = _thisInstrument.GetNoteClip(playbackSettings.Note);
            _hasScheduledPlay = true;
            IsReady = false;
            //StopScheduled(playbackSettings.StopTime);
            //NextPlaybackSettings.StopTime = playbackSettings.StopTime;
        }

        

        public override float GetDurationToEnd()
        {
            if (!_currentNoteClip) return 0;

            var timeToPlay = (float)(ScheduledPlayTime - AudioSettings.dspTime);
            timeToPlay = Mathf.Max(timeToPlay, 0);
            return timeToPlay + (float)(_currentNoteClip.clipSamples.Length - _samplePosBuffer1);
        }


        private void StopScheduled(double absoluteTime)
        {
            
        }


        private void PlayScheduled(PlaybackSettings nextUp, AnywhenNoteClip noteClip)
        {
            
        }


        void InitPlay()
        {
            CurrentPlaybackSettings = NextPlaybackSettings;
            _currentNoteClip = _nextNoteClip;
            
            
            if (_currentNoteClip == null) return;
            _samplePosBuffer1 = 0;

            _sampleStepFrac = _currentNoteClip.frequency / _currentSampleRate;
            _currentPitch = 1;
            if (_thisInstrument.TempoControlPitch)
            {
                _currentPitch = _thisInstrument.GetPitchFromTempo(AnywhenMetronome.Instance.GetTempo());
            }

            CurrentPlaybackSettings.Pitch = 1;

            _isPlaying = true;
            _hasScheduledPlay = false;

            _adsr.SetGate(true);

            if (_currentTrack.pitchLFOSettings is { enabled: true, retrigger: true }) _pitchLFO.NoteOn();
        }


        void SetEnvelope(AnywhenSampleInstrument.EnvelopeSettings envelopeSettings)
        {
            _adsr.SetAttackRate(envelopeSettings.attack * _currentSampleRate);
            _adsr.SetDecayRate(envelopeSettings.decay * _currentSampleRate);
            _adsr.SetReleaseRate(envelopeSettings.release * _currentSampleRate);
            _adsr.SetSustainLevel(envelopeSettings.sustain);
            _adsr.Reset();
        }

        void SetPitchLFO(AnywhenSampleInstrument.PitchLFOSettings pitchLFOSettings)
        {
            _pitchLFO.UpdateSettings(pitchLFOSettings);
        }


        float[] DSP_WriteToBuffer(float[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                _ampMod = 1;

                _ampMod *= _adsr.Process();

                if (_currentTrack.pitchLFOSettings.enabled)
                {
                    _pitchLFO.DoUpdate();
                    _currentPitch = _pitchLFO.Process();
                }

                int sampleIndex1 = (int)_samplePosBuffer1;
                double f1 = _samplePosBuffer1 - sampleIndex1;
                var sourceSample1 = Mathf.Min((sampleIndex1), _currentNoteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex1) + 1, _currentNoteClip.clipSamples.Length - 1);
                double e1 = ((1 - f1) * _currentNoteClip.clipSamples[sourceSample1]) +
                            (f1 * _currentNoteClip.clipSamples[sourceSample2]);

                data[i] = ((float)(e1)) * _ampMod * _thisInstrument.volume * CurrentPlaybackSettings.Volume;

                _samplePosBuffer1 += (_sampleStepFrac * _currentPitch) / 2f;


                i++;
            }

            return data;
        }


        private void SetReady()
        {
            IsReady = true;
            _hasScheduledPlay = false;
            _currentNoteClip = null;
            _isPlaying = false;
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] data = new float[bufferSize];


            if (_hasScheduledPlay && AudioSettings.dspTime >= NextPlaybackSettings.PlayTime)
            {
                InitPlay();
            }


            if (!_isPlaying) return data;

            if (CurrentPlaybackSettings.StopTime >= 0 && AudioSettings.dspTime > CurrentPlaybackSettings.StopTime)
            {
                CurrentPlaybackSettings.StopTime = -1;
                _adsr.SetGate(false);
            }


            if (_adsr.IsIdle)
            {
                SetReady();
                return data;
            }

            if (!_currentNoteClip)
                return data;


            if (_samplePosBuffer1 >= _currentNoteClip.clipSamples.Length)
            {
                _adsr.SetGate(false);
                SetReady();
                return data;
            }

            return DSP_WriteToBuffer(data);
        }
    }
}