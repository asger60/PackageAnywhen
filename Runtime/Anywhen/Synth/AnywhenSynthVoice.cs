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
using Anywhen.SettingsObjects;
using Anywhen.Synth.Filter;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.Synth
{
    public class AnywhenSynthVoice : AnywhenVoiceBase
    {
        [Serializable]
        struct SynthVoiceGroup
        {
            public SynthOscillator[] oscillators;
        }

        private SynthVoiceGroup[] _voices;


        //private SynthOscillator[] _oscillators;


        private SynthControlBase[] _voiceFrequencyModifiers;
        private SynthControlBase[] _amplitudeModifiers;
        private SynthControlBase[] _filterModifiers;


        private SynthFilterBase[] _filters;

        private bool _isInitialized = false;

        // Current MIDI evt
        private int _currentVelocity;

        private EventQueue _noteOnQueue;


        public static float[] FreqTab;

        private const int QueueCapacity = 16;
        private readonly float[] _lastBuffer = new float[2048];
        private readonly object _bufferMutex = new object();
        //private bool _debugBufferEnabled = false;

        private EventQueue.QueuedEvent _nextEvent;
        private EventQueue.QueuedEvent _offEvent;

        private NoteEvent _currentNoteEvent;
        private bool _noteOnWaiting;
        private bool _noteOffWaiting;

        [SerializeField] private AnywhenSynthPreset _preset;
        public AnywhenSynthPreset Preset => _preset;
        private int _noteOnCount;
        private bool _isCreated;

        /// Public interface
        public void HandleEventScheduled(NoteEvent noteEvent, double scheduledPlayTime)
        {
            if (noteEvent.state == NoteEvent.EventTypes.NoteOn)
            {
                _noteOnQueue.Enqueue(noteEvent, scheduledPlayTime);
                _noteOnCount++;
            }
            else
            {
                _offEvent = new EventQueue.QueuedEvent();
                _offEvent.Set(noteEvent, scheduledPlayTime);
                _noteOffWaiting = true;
            }
        }

        public override void NoteOn(int note, double playTime, double duration, float volume, AnywhenInstrument instrument,
            AnywhenSampleInstrument.EnvelopeSettings envelope)
        {
        }


        public void SetPreset(AnywhenSynthPreset preset)
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
                _amplitudeModifiers = null;
                _filterModifiers = null;
                _filters = null;
            }
        }


        public void RebuildSynth()
        {
            _voices = new SynthVoiceGroup[_preset.oscillatorSettings.Length];
            _voiceFrequencyModifiers = new SynthControlBase[_preset.pitchModifiers.Length];
            _amplitudeModifiers = new SynthControlBase[_preset.amplitudeModifiers.Length];
            _filterModifiers = new SynthControlBase[_preset.filterModifiers.Length];
            _filters = new SynthFilterBase[_preset.filterSettings.Length];

            for (int i = 0; i < _preset.oscillatorSettings.Length; i++)
            {
                var oscillatorSetting = _preset.oscillatorSettings[i];

                bool isNoise = oscillatorSetting.oscillatorType == SynthSettingsObjectOscillator.OscillatorType.Noise;
                _voices[i].oscillators = new SynthOscillator[isNoise ? 1 : _preset.voices];

                for (int j = 0; j < _voices[i].oscillators.Length; j++)
                {
                    var newOSC = new SynthOscillator();
                    newOSC.UpdateSettings(oscillatorSetting);
                    _voices[i].oscillators[j] = newOSC;
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

                        var newHPFilter = new SynthFilterBandPass(_sampleRate);
                        newHPFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newHPFilter;

                        break;
                    case SynthSettingsObjectFilter.FilterTypes.Formant:

                        var newFormantFilter = new SynthFilterBandPass(_sampleRate);
                        newFormantFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newFormantFilter;

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

            for (int i = 0; i < _preset.amplitudeModifiers.Length; i++)
            {
                var ampModSetting = _preset.amplitudeModifiers[i];
                switch (ampModSetting)
                {
                    case SynthSettingsObjectLFO lfo:
                    {
                        var newController = new SynthControlLFO();
                        newController.UpdateSettings(lfo);
                        _amplitudeModifiers[i] = newController;
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var newController = new SynthControlEnvelope();
                        newController.UpdateSettings(envelope);
                        _amplitudeModifiers[i] = newController;
                        break;
                    }
                }
            }

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
                foreach (var synthOscillator in voice.oscillators)
                {
                    synthOscillator.Init();
                }
            }

            _noteOnCount = 0;
            _noteOnWaiting = false;
            _noteOffWaiting = false;
        }


        void HandleQueue(int queueSize)
        {
            // Event handling
            // This is sample accurate event handling.
            // If it's too slow, we can decide to only handle 1 event per buffer and
            // move this code outside the loop.
            while (true)
            {
                if (_noteOffWaiting && _offEvent.EventTime <= AudioSettings.dspTime)
                {
                    _offEvent.NoteEvent.velocity = _currentNoteEvent.velocity;
                    HandleEventNow(_offEvent);
                    _noteOffWaiting = false;
                }

                if (!_noteOnWaiting && _noteOnCount > 0)
                {
                    if (_noteOnQueue.GetFrontAndDequeue(ref _nextEvent))
                    {
                        _noteOnWaiting = true;
                        _noteOnCount--;
                    }
                }

                if (_noteOnWaiting)
                {
                    if (_nextEvent.EventTime <= AudioSettings.dspTime)
                    {
                        HandleEventNow(_nextEvent);
                        _noteOnWaiting = false;
                    }
                    else
                    {
                        // we assume that queued events are in order, so if it's not
                        // now, we stop getting events from the queue
                        break;
                    }
                }
                else
                {
                    // no more events
                    break;
                }
            }
        }

        // This should only be called from OnAudioFilterRead
        private void HandleEventNow(EventQueue.QueuedEvent currentEvent)
        {
            _currentNoteEvent = currentEvent.NoteEvent;
            if (currentEvent.NoteEvent.state == NoteEvent.EventTypes.NoteOn)
            {
                ResetVoices();
                if (_preset.unison)
                {
                    foreach (var voice in _voices)
                    {
                        for (int i = 0; i < voice.oscillators.Length; i++)
                        {
                            var osc = voice.oscillators[i];
                            osc.SetNote(_currentNoteEvent.notes[0], AnywhenRuntime.SampleRate);
                            osc.SetFineTuning(i * _preset.voiceSpread, AnywhenRuntime.SampleRate);
                        }
                    }
                }
                else
                {
                    foreach (var voice in _voices)
                    {
                        for (int i = 0; i < _currentNoteEvent.notes.Length; i++)
                        {
                            if (i >= voice.oscillators.Length)
                            {
                                break;
                            }

                            var osc = voice.oscillators[i];
                            int currentNote = _currentNoteEvent.notes[i];


                            osc.SetFineTuning(i * _preset.voiceSpread, AnywhenRuntime.SampleRate);
                            osc.SetNote(currentNote, AnywhenRuntime.SampleRate);
                        }
                    }
                }


                foreach (var ampModifier in _amplitudeModifiers)
                {
                    ampModifier.NoteOn();
                }

                foreach (var filterModifier in _filterModifiers)
                {
                    filterModifier.NoteOn();
                }

                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    frequencyModifier.NoteOn();
                }
            }
            else if (currentEvent.NoteEvent.state == NoteEvent.EventTypes.NoteOff)
            {
                foreach (var ampModifier in _amplitudeModifiers)
                {
                    ampModifier.NoteOff();
                }

                foreach (var filterModifier in _filterModifiers)
                {
                    filterModifier.NoteOff();
                }

                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    frequencyModifier.NoteOff();
                }
            }
        }


        //private void Start()
        //{
        //    Init();
        //}


        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_isInitialized)
            {
                if (channels == 2)
                {
                    // Cache this for the entire buffer, we don't need to check for
                    // every sample if new events have been enqueued.
                    // This assumes that no other methods call GetFrontAndDequeue.
                    HandleQueue(_noteOnQueue.GetSize());

                    int sampleFrames = data.Length / 2;

                    RenderFloat32StereoInterleaved(data, sampleFrames);

                    //if (_debugBufferEnabled)
                    //{
                    //    lock (_bufferMutex)
                    //    {
                    //        Array.Copy(data, _lastBuffer, data.Length);
                    //    }
                    //}
                }
            }
        }


        int _sampleRate;

        /// Internal
        public override void Init(int sampleRate)
        {
            _sampleRate = sampleRate;


            if (FreqTab == null)
            {
                FreqTab = new float[128];
                for (int i = 0; i < 128; i++)
                {
                    // 128 midi notes
                    FreqTab[i] = Midi2Freq(i);
                }
            }

            _noteOnQueue = new EventQueue(QueueCapacity);
            RebuildSynth();

            ResetVoices();
            _isInitialized = true;
        }


        private void ResetVoices()
        {
            if (_voices == null) return;
            foreach (var voice in _voices)
            {
                foreach (var synthOscillator in voice.oscillators)
                {
                    synthOscillator.ResetPhase();
                    synthOscillator.SetInactive();
                }
            }
        }

        private float[] RenderFloat32StereoInterleaved(float[] buffer, int sampleFrames)
        {
            if (!_isInitialized) return buffer;

            int smp = 0;
            int bufferIndex = 0;




            // Render loop
            for (; smp < sampleFrames; ++smp)
            {
                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    frequencyModifier.DoUpdate();
                }

                foreach (var amplitudeModifier in _amplitudeModifiers)
                {
                    amplitudeModifier.DoUpdate();
                }

                foreach (var filterModifier in _filterModifiers)
                {
                    filterModifier.DoUpdate();
                }


                float ampMod = 1;
                foreach (var ampModifier in _amplitudeModifiers)
                {
                    ampMod *= ampModifier.Process();
                }

                ampMod *= _currentNoteEvent.velocity;


                float voiceFreqMod = 1;
                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    voiceFreqMod *= frequencyModifier.Process();
                }


                float oscillatorOutput = 0;
                int numOsc = 0;
                foreach (var voice in _voices)
                {
                    foreach (var synthOscillator in voice.oscillators)
                    {
                        if (!synthOscillator.IsActive) break;

                        synthOscillator.SetPitchMod(voiceFreqMod, AnywhenRuntime.SampleRate);
                        oscillatorOutput += synthOscillator.Process();
                        numOsc++;
                    }

                    oscillatorOutput /= numOsc;
                }

                float sample = oscillatorOutput * ampMod;
                // Filter entire buffer
                for (var i = 0; i < _filters.Length; i++)
                {
                    var audioFilterBase = _filters[i];
                    audioFilterBase.SetParameters(_preset.filterSettings[i]);
                    sample = audioFilterBase.Process(sample);
                }

                buffer[bufferIndex++] = sample;
                buffer[bufferIndex++] = sample;


                // Update oscillators
                foreach (var voice in _voices)
                {
                    foreach (var synthOscillator in voice.oscillators)
                    {
                        synthOscillator.DoUpdate();
                    }
                }


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

            return buffer;
        }


        /// Internals
        private static float Midi2Freq(int note)
        {
            return 440 * Mathf.Pow(2, (note - 69) / 12f);
        }

        public override float[] UpdateDSP(int bufferSize, int channels)
        {
            if (!_isInitialized) return new float[bufferSize];

            if (channels == 2)
            {
                // Cache this for the entire buffer, we don't need to check for
                // every sample if new events have been enqueued.
                // This assumes that no other methods call GetFrontAndDequeue.
                HandleQueue(_noteOnQueue.GetSize());

                int sampleFrames = bufferSize / 2;

                return RenderFloat32StereoInterleaved(new float[bufferSize], sampleFrames);

                //if (_debugBufferEnabled)
                //{
                //    lock (_bufferMutex)
                //    {
                //        Array.Copy(data, _lastBuffer, data.Length);
                //    }
                //}
            }

            return new float[bufferSize];
        }
    }
}