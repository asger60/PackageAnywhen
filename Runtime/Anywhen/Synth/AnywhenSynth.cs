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
using UnityEngine;
using UnitySynth.Runtime.Synth;
using UnitySynth.Runtime.Synth.Filter;

namespace Anywhen.Synth.Synth
{
    [RequireComponent(typeof(AudioSource))]
    public class AnywhenSynth : MonoBehaviour
    {
        [Serializable]
        struct SynthVoiceGroup
        {
            public SynthOscillator[] _oscillators;
        }

        private SynthVoiceGroup[] _voices;


        //private SynthOscillator[] _oscillators;


        private SynthControlBase[] _voiceFrequencyModifiers;

        SynthControlBase[] amplitudeModifiers;

        private SynthControlBase[] _filterModifiers;


        private SynthFilterBase[] _filters;

        private bool _isInitialized = false;

        // Current MIDI evt
        private int _currentVelocity;

        private EventQueue _queue;


        public static float[] FreqTab;
        private float[] _freqTabTest;

        private const int QueueCapacity = 320;
        private readonly float[] _lastBuffer = new float[2048];
        private readonly object _bufferMutex = new object();
        private bool _debugBufferEnabled = false;

        private EventQueue.QueuedEvent _nextEvent;
        private NoteEvent _currentNoteEvent;
        private bool _eventIsWaiting = false;
        [SerializeField] private AnywhenSynthPreset _preset;
        public AnywhenSynthPreset Preset => _preset;
        public static int SampleRate;

        /// Public interface
        public void HandleEventScheduled(NoteEvent noteEvent, double scheduledPlayTime)
        {
            //queueLock = true;
            if (noteEvent.state == NoteEvent.EventTypes.NoteOff)
            {
                noteEvent.velocity = _currentNoteEvent.velocity;

                _eventIsWaiting = false;
            }


            _queue.Enqueue(noteEvent, scheduledPlayTime);
        }


        public void ClearQueue()
        {
            _queue.Clear();
        }


        public void SetPreset(AnywhenSynthPreset preset)
        {
            _preset = preset;
            _preset.BindToRuntime(this);
            RebuildSynth();
        }

        public void RebuildSynth()
        {
            foreach (var synthControlBase in GetComponentsInChildren<SynthControlBase>())
            {
                Destroy(synthControlBase.gameObject);
            }

            foreach (var synthOscillator in GetComponentsInChildren<SynthOscillator>())
            {
                Destroy(synthOscillator.gameObject);
            }

            foreach (var synthFilterBase in GetComponentsInChildren<SynthFilterBase>())
            {
                Destroy(synthFilterBase.gameObject);
            }


            _voices = new SynthVoiceGroup[_preset.oscillatorSettings.Length];
            _voiceFrequencyModifiers = new SynthControlBase[_preset.pitchModifiers.Length];
            amplitudeModifiers = new SynthControlBase[_preset.amplitudeModifiers.Length];
            _filterModifiers = new SynthControlBase[_preset.filterModifiers.Length];
            _filters = new SynthFilterBase[_preset.filterSettings.Length];

            for (int i = 0; i < _preset.oscillatorSettings.Length; i++)
            {
                var oscillatorSetting = _preset.oscillatorSettings[i];


                var go = new GameObject("Oscillator " + oscillatorSetting.oscillatorType);
                go.transform.SetParent(transform);
                bool isNoise = oscillatorSetting.oscillatorType == SynthSettingsObjectOscillator.OscillatorType.Noise;
                _voices[i]._oscillators = new SynthOscillator[isNoise ? 1 : _preset.voices];

                for (int j = 0; j < _voices[i]._oscillators.Length; j++)
                {
                    var newOSC = go.AddComponent<SynthOscillator>();
                    newOSC.UpdateSettings(oscillatorSetting);
                    _voices[i]._oscillators[j] = newOSC;
                }
            }

            for (int i = 0; i < _preset.filterSettings.Length; i++)
            {
                switch (_preset.filterSettings[i].filterType)
                {
                    case SynthSettingsObjectFilter.FilterTypes.LowPass:

                        var modGO = new GameObject("LowPassFilter");
                        modGO.transform.SetParent(transform);
                        var newFilter = modGO.AddComponent<SynthFilterLowPass>();
                        newFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newFilter;


                        break;
                    case SynthSettingsObjectFilter.FilterTypes.BandPass:

                        var phFilterGO = new GameObject("BandPassFilter");
                        phFilterGO.transform.SetParent(transform);
                        var newHPFilter = phFilterGO.AddComponent<SynthFilterBandPass>();
                        newHPFilter.SetSettings(_preset.filterSettings[i]);
                        _filters[i] = newHPFilter;

                        break;
                    case SynthSettingsObjectFilter.FilterTypes.Formant:

                        var formantFilterGO = new GameObject("FormantFilter");
                        formantFilterGO.transform.SetParent(transform);
                        var newFormantFilter = formantFilterGO.AddComponent<SynthFilterFormant>();
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
                        var modGO = new GameObject("Frequency Modifier LFO");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlLFO>();
                        newController.UpdateSettings(lfo);
                        _voiceFrequencyModifiers[i] = newController;
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var modGO = new GameObject("Frequency Modifier Envelope");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlEnvelope>();
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
                        var modGO = new GameObject("Amplitude Modifier LFO");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlLFO>();
                        newController.UpdateSettings(lfo);
                        amplitudeModifiers[i] = newController;
                        print("create lfo");
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var modGO = new GameObject("Amplitude Modifier Envelope");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlEnvelope>();
                        newController.UpdateSettings(envelope);
                        amplitudeModifiers[i] = newController;
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
                        var modGO = new GameObject("Filter Modifier LFO");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlLFO>();
                        newController.UpdateSettings(lfo);
                        _filterModifiers[i] = newController;
                        break;
                    }
                    case SynthSettingsObjectEnvelope envelope:
                    {
                        var modGO = new GameObject("Filter Modifier Envelope");
                        modGO.transform.SetParent(transform);
                        var newController = modGO.AddComponent<SynthControlEnvelope>();
                        newController.UpdateSettings(envelope);
                        _filterModifiers[i] = newController;
                        break;
                    }
                }
            }

            _isInitialized = true;
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
                        for (int i = 0; i < voice._oscillators.Length; i++)
                        {
                            var osc = voice._oscillators[i];
                            osc.SetNote(_currentNoteEvent.notes[0], SampleRate);
                            osc.SetFineTuning(i * _preset.voiceSpread, SampleRate);
                        }
                    }
                }
                else
                {
                    foreach (var voice in _voices)
                    {
                        for (int i = 0; i < _currentNoteEvent.notes.Length; i++)
                        {
                            if (i >= voice._oscillators.Length)
                            {
                                break;
                            }

                            var osc = voice._oscillators[i];
                            int currentNote = _currentNoteEvent.notes[i];


                            osc.SetFineTuning(i * _preset.voiceSpread, SampleRate);
                            osc.SetNote(currentNote, SampleRate);
                        }
                    }
                }


                foreach (var ampModifier in amplitudeModifiers)
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
                foreach (var ampModifier in amplitudeModifiers)
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


        /// Debug
        public void SetDebugBufferEnabled(bool enabled)
        {
            this._debugBufferEnabled = enabled;
        }

        public float[] GetLastBuffer()
        {
            return _lastBuffer;
        }

        public object GetBufferMutex()
        {
            return _bufferMutex;
        }

        /// Unity
        private void Start()
        {
            Init();
        }


        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_isInitialized)
            {
                if (channels == 2)
                {
                    int sampleFrames = data.Length / 2;

                    RenderFloat32StereoInterleaved(data, sampleFrames);

                    if (_debugBufferEnabled)
                    {
                        lock (_bufferMutex)
                        {
                            Array.Copy(data, _lastBuffer, data.Length);
                        }
                    }
                }
            }
        }

        /// Internal
        private void Init()
        {
            SampleRate = AudioSettings.outputSampleRate;
            if (FreqTab == null)
            {
                _freqTabTest = new float[128];
                FreqTab = new float[128];
                for (int i = 0; i < 128; i++)
                {
                    // 128 midi notes
                    FreqTab[i] = Midi2Freq(i);
                    _freqTabTest[i] = Midi2Freq(i);
                }
            }

            _queue = new EventQueue(QueueCapacity);


            ResetVoices();
        }

        private void ResetVoices()
        {
            if (_voices == null) return;
            foreach (var voice in _voices)
            {
                foreach (var synthOscillator in voice._oscillators)
                {
                    synthOscillator.ResetPhase();
                    synthOscillator.SetInactive();
                }
            }
        }


        private void RenderFloat32StereoInterleaved(float[] buffer, int sampleFrames)
        {
            if (!_isInitialized) return;

            int smp = 0;
            int bufferIndex = 0;


            // Cache this for the entire buffer, we don't need to check for
            // every sample if new events have been enqueued.
            // This assumes that no other methods call GetFrontAndDequeue.
            int queueSize = _queue.GetSize();

            // Render loop
            for (; smp < sampleFrames; ++smp)
            {
                // Event handling
                // This is sample accurate event handling.
                // If it's too slow, we can decide to only handle 1 event per buffer and
                // move this code outside the loop.
                while (true)
                {
                    if (_eventIsWaiting == false && queueSize > 0)
                    {
                        //queueLock = true;
                        if (_queue.GetFrontAndDequeue(ref _nextEvent))
                        {
                            _eventIsWaiting = true;
                            queueSize--;
                        }
                        //queueLock = false;
                    }

                    if (_eventIsWaiting)
                    {
                        //if (nextEvent.time_smp <= time_smp)
                        if (_nextEvent.eventTime <= AudioSettings.dspTime)
                        {
                            HandleEventNow(_nextEvent);
                            _eventIsWaiting = false;
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

                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    frequencyModifier.DoUpdate();
                }

                foreach (var amplitudeModifier in amplitudeModifiers)
                {
                    amplitudeModifier.DoUpdate();
                }

                foreach (var filterModifier in _filterModifiers)
                {
                    filterModifier.DoUpdate();
                }


                float ampMod = _currentNoteEvent.velocity;
                foreach (var ampModifier in amplitudeModifiers)
                {
                    ampMod *= ampModifier.Process();
                }


                float voiceFreqMod = 1;
                foreach (var frequencyModifier in _voiceFrequencyModifiers)
                {
                    voiceFreqMod *= frequencyModifier.Process();
                }


                float oscillatorOutput = 0;
                int numOsc = 0;
                foreach (var voice in _voices)
                {
                    foreach (var synthOscillator in voice._oscillators)
                    {
                        if (!synthOscillator.IsActive) break;

                        synthOscillator.SetPitchMod(voiceFreqMod, SampleRate);
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
                    foreach (var synthOscillator in voice._oscillators)
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


            // Filter entire buffer
            //for (var i = 0; i < _filters.Length; i++)
            //{
            //    var audioFilterBase = _filters[i];
            //    audioFilterBase.SetParameters(_preset.filterSettings[i / 2]);
            //    audioFilterBase.process_mono_stride(buffer, sampleFrames, i % 2, 2);
            //}
        }


        /// Internals
        private static float Midi2Freq(int note)
        {
            return 440 * (Mathf.Pow(2, ((note - 69) / 12f)));
            // return 32.70319566257483f * Mathf.Pow(2.0f, note / 12.0f + octave);
        }
    }
}