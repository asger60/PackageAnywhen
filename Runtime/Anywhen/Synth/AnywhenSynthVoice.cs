// Copyright (c) 2018 Jakob Schmid
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE."

using System;
using Anywhen.Composing;
using Anywhen.Synth.Filter;
using UnityEngine;

namespace Anywhen.Synth
{
    [Serializable]
    public class AnywhenSynthVoice : AnywhenVoiceBase
    {
        [Serializable]
        struct SynthVoiceGroup
        {
            public SynthOscillator[] Oscillators;
        }

        private SynthVoiceGroup[] _voices;

        private SynthControlBase[] _voiceFrequencyModifiers;

        //private SynthControlBase[] _amplitudeModifiers;
        private SynthControlBase[] _filterModifiers;

        private SynthFilterBase[] _filters;

        private bool _isInitialized;

        // Current MIDI evt
        private int _currentVelocity;

        public static float[] FreqTab;

        private AnywhenSynthPreset _preset;

        private bool _isCreated;

        public AnywhenSynthVoice(AnywhenInstrument instrumentSettings, AnysongTrack trackSettings) : base(instrumentSettings, trackSettings)
        {
            InitializeFreqTab();

            SetPreset(instrumentSettings as AnywhenSynthPreset);

            RebuildSynth();
            ResetVoices();
            _isInitialized = true;
        }

        private static void InitializeFreqTab()
        {
            if (FreqTab == null)
            {
                FreqTab = new float[128];
                for (int i = 0; i < 128; i++)
                {
                    FreqTab[i] = Midi2Freq(i);
                }
            }
        }


        private void SetPreset(AnywhenSynthPreset preset)
        {
            _isInitialized = false;

            _preset = preset;

            if (_preset)
            {
                _preset.BindToRuntime(this);
                RebuildSynth();
            }
            else
            {
                _isInitialized = false;
                _preset = null;
                _voices = null;
                _voiceFrequencyModifiers = null;
                //_amplitudeModifiers = null;
                _filterModifiers = null;
                _filters = null;
            }
        }


        public void RebuildSynth()
        {
            _voices = new SynthVoiceGroup[_preset.oscillatorSettings.Length];
            _voiceFrequencyModifiers = new SynthControlBase[_preset.pitchModifiers.Length];
            //_amplitudeModifiers = new SynthControlBase[_preset.amplitudeModifiers.Length];
            _filterModifiers = new SynthControlBase[_preset.filterModifiers.Length];
            _filters = new SynthFilterBase[_preset.filterSettings.Length];

            for (int i = 0; i < _preset.oscillatorSettings.Length; i++)
            {
                var oscillatorSetting = _preset.oscillatorSettings[i];

                bool isNoise = oscillatorSetting.oscillatorType == SynthSettingsObjectOscillator.OscillatorType.Noise;
                _voices[i].Oscillators = new SynthOscillator[isNoise ? 1 : _preset.voices];

                for (int j = 0; j < _voices[i].Oscillators.Length; j++)
                {
                    var newOsc = new SynthOscillator();
                    newOsc.UpdateSettings(oscillatorSetting);
                    _voices[i].Oscillators[j] = newOsc;
                }
            }

            for (int i = 0; i < _preset.filterSettings.Length; i++)
            {
                switch (_preset.filterSettings[i].filterType)
                {
                    case SynthSettingsObjectFilter.FilterTypes.LowPass:
                        var newFilter = new SynthFilterLowPass();
                        newFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newFilter;
                        break;
                    case SynthSettingsObjectFilter.FilterTypes.BandPass:
                        var newHpFilter = new SynthFilterBandPass();
                        newHpFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newHpFilter;
                        break;
                    case SynthSettingsObjectFilter.FilterTypes.Formant:
                        var newFormantFilter = new SynthFilterFormant();
                        newFormantFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newFormantFilter;
                        break;
                    case SynthSettingsObjectFilter.FilterTypes.Ladder:
                        var newLadderFilter = new SynthFilterLadder();
                        newLadderFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newLadderFilter;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            for (int i = 0; i < _preset.pitchModifiers.Length; i++)
            {
                var freqMod = _preset.pitchModifiers[i];
                switch (freqMod)
                {
                    case SynthSettingsObjectLFO lfo:
                    {
                        var newController = new SynthControlLFO();
                        newController.UpdateSettings(lfo);
                        _voiceFrequencyModifiers[i] = newController;
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var newController = new SynthControlEnvelope();
                        newController.UpdateSettings(envelope);
                        _voiceFrequencyModifiers[i] = newController;
                        break;
                    }
                }
            }

            //for (int i = 0; i < _preset.amplitudeModifiers.Length; i++)
            //{
            //    var ampModSetting = _preset.amplitudeModifiers[i];
            //    switch (ampModSetting)
            //    {
            //        case SynthSettingsObjectLFO lfo:
            //        {
            //            var newController = new SynthControlLFO();
            //            newController.UpdateSettings(lfo);
            //            _amplitudeModifiers[i] = newController;
            //            break;
            //        }
            //        case SynthSettingsObjectEnvelope envelope:
            //        {
            //            var newController = new SynthControlEnvelope();
            //            newController.UpdateSettings(envelope);
            //            _amplitudeModifiers[i] = newController;
            //            break;
            //        }
            //    }
            //}

            for (int i = 0; i < _preset.filterModifiers.Length; i++)
            {
                var filterModifier = _preset.filterModifiers[i];
                switch (filterModifier)
                {
                    case SynthSettingsObjectLFO lfo:
                    {
                        var newController = new SynthControlLFO();
                        newController.UpdateSettings(lfo);
                        _filterModifiers[i] = newController;
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var newController = new SynthControlEnvelope();
                        newController.UpdateSettings(envelope);
                        _filterModifiers[i] = newController;
                        break;
                    }
                }
            }

            foreach (var voice in _voices)
            {
                foreach (var synthOscillator in voice.Oscillators)
                {
                    synthOscillator.Init();
                }
            }
        }


        private void ResetVoices()
        {
            if (_voices == null) return;
            foreach (var voice in _voices)
            {
                foreach (var synthOscillator in voice.Oscillators)
                {
                    synthOscillator.ResetPhase();
                    synthOscillator.SetInactive();
                }
            }
        }


        /// Internals
        private static float Midi2Freq(int note)
        {
            return 440 * Mathf.Pow(2, (note - 69) / 12f);
        }


        protected override void StartPlay(PlaybackSettings playbackSettings)
        {
            InitializeFreqTab();
            base.StartPlay(playbackSettings);

            ResetVoices();

            foreach (var voice in _voices)
            {
                for (var i = 0; i < voice.Oscillators.Length; i++)
                {
                    var osc = voice.Oscillators[i];
                    osc.SetNote(AnywhenRuntime.Conductor.GetScaledNote(CurrentPlaybackSettings.note, 64));
                    osc.SetFineTuning(i * _preset.voiceSpread);
                }
            }

            //foreach (var ampModifier in _amplitudeModifiers)
            //{
            //    ampModifier.NoteOn();
            //}

            foreach (var filterModifier in _filterModifiers)
            {
                filterModifier.NoteOn();
            }

            foreach (var frequencyModifier in _voiceFrequencyModifiers)
            {
                frequencyModifier.NoteOn();
            }
        }


        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            float[] buffer = new float[bufferSize];
            if (!_isInitialized) return buffer;
            HandleQueue();

            if (!IsPlaying)
            {
                ResetVoices();
                return buffer;
            }


            if (channels == 2)
            {
                int sampleFrames = bufferSize / 2;
                int bufferIndex = 0;


                // Render loop
                for (int smp = 0; smp < sampleFrames; ++smp)
                {
                    // Update modulators
                    foreach (var frequencyModifier in _voiceFrequencyModifiers)
                    {
                        frequencyModifier.DoUpdate();
                    }

                    foreach (var filterModifier in _filterModifiers)
                    {
                        filterModifier.DoUpdate();
                    }

                    // Calculate modulation values
                    float ampMod = 1;
                    ampMod *= AmplitudeEnvelope.Process() * CurrentPlaybackSettings.volume;
                    //foreach (var ampModifier in _amplitudeModifiers)
                    //{
                    //    ampMod *= ampModifier.Process() * CurrentPlaybackSettings.Volume;
                    //}


                    if (CurrentTrack.pitchLFOSettings.enabled)
                    {
                        PitchLFO.DoUpdate();
                        CurrentPitch = PitchLFO.Process();
                    }



                    float voiceFreqMod = 1;
                    foreach (var frequencyModifier in _voiceFrequencyModifiers)
                    {
                        float mod = frequencyModifier.Process();
                        if (!float.IsNaN(mod) && !float.IsInfinity(mod))
                            voiceFreqMod *= mod;
                    }

                    //voiceFreqMod *= (float)CurrentPitch;

                    // Guard against NaN/Inf in modulation
                    if (float.IsNaN(voiceFreqMod) || float.IsInfinity(voiceFreqMod)) voiceFreqMod = 1;

                    // Generate oscillator output
                    float oscillatorOutput = 0;
                    int totalActiveOsc = 0;

                    foreach (var voice in _voices)
                    {
                        foreach (var synthOscillator in voice.Oscillators)
                        {
                            if (!synthOscillator.IsActive) continue;

                            synthOscillator.SetPitchMod(voiceFreqMod);
                            synthOscillator.SetPitchRaw((float)CurrentPitch * CurrentTrack.TrackPitch);
                            oscillatorOutput += synthOscillator.Process();
                            totalActiveOsc++;
                        }
                    }

                    // Avoid division by zero
                    if (totalActiveOsc > 0)
                    {
                        oscillatorOutput /= totalActiveOsc;
                    }

                    if (float.IsNaN(oscillatorOutput) || float.IsInfinity(oscillatorOutput)) oscillatorOutput = 0;

                    float sample = oscillatorOutput * ampMod * CurrentTrack.volume;

                    // Apply filters
                    for (var i = 0; i < _filters.Length; i++)
                    {
                        var audioFilterBase = _filters[i];
                        audioFilterBase.SetParameters(_preset.filterSettings[i]);
                        sample = audioFilterBase.Process(sample);
                    }

                    if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0;

                    buffer[bufferIndex++] = sample;
                    buffer[bufferIndex++] = sample;

                    // Update oscillator phases
                    foreach (var voice in _voices)
                    {
                        foreach (var synthOscillator in voice.Oscillators)
                        {
                            synthOscillator.DoUpdate();
                        }
                    }

                    // Handle filter modulation
                    foreach (var audioFilterBase in _filters)
                    {
                        float currentMod = 1;
                        foreach (var filterModifier in _filterModifiers)
                        {
                            currentMod *= filterModifier.Process();
                        }

                        audioFilterBase.HandleModifiers(currentMod);
                    }
                }
            }

            if (AmplitudeEnvelope.IsIdle && !HasScheduledPlay)
            {
                SetReady();
                ResetVoices();
            }

            return buffer;
        }
    }
}