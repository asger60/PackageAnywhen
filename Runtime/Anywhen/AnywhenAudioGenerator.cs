using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "AnywhenAudioPlayer", menuName = "Anywhen/Create AnywhenAudioPlayer asset", order = 2)]
public class AnywhenAudioGenerator : ScriptableObject, IAudioGenerator
{
    [SerializeField] AnysongObject song;

    private bool _sectionLockState;
    int _currentLockSectionIndex;
    private bool _isPlaying;
    private GeneratorInstance _generatorInstance;

    private void OnEnable()
    {
        OnAudioGeneratedStatic += HandleAudioGeneratedStatic;
        
    }

    private void OnDisable()
    {
        OnAudioGeneratedStatic -= HandleAudioGeneratedStatic;
        UnregisterSongListeners();

        if (_sharedStepIndices.IsCreated) _sharedStepIndices.Dispose();
        if (_sharedPatternIndices.IsCreated) _sharedPatternIndices.Dispose();
        if (_sharedSectionIndices.IsCreated) _sharedSectionIndices.Dispose();
    }

    private void RegisterSongListeners()
    {
        if (song == null) return;
        song.OnSongMidiChanged += HandleSongMidiChanged;
        song.OnSongSectionsChanged += HandleSongSectionsChanged;
        song.OnSongSettingsChanged += HandleSongSettingsChanged;
        song.OnSongEffectsChanged += HandleSongEffectsChanged;
        song.OnSongTracksChanged += HandleSongTracksChanged;
    }

    private void UnregisterSongListeners()
    {
        if (song == null) return;
        song.OnSongMidiChanged -= HandleSongMidiChanged;
        song.OnSongSectionsChanged -= HandleSongSectionsChanged;
        song.OnSongSettingsChanged -= HandleSongSettingsChanged;
        song.OnSongEffectsChanged -= HandleSongEffectsChanged;
        song.OnSongTracksChanged -= HandleSongTracksChanged;
    }

    private void HandleSongMidiChanged(int sectionIndex, int trackIndex, int patternIndex)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            var sectionData = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Temp);
            for (int i = 0; i < song.Sections.Count; i++)
            {
                sectionData[i] = song.Sections[i].ToUnmanaged();
            }

            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerMidiReloadMsg(sectionData));
        }
    }

    private void HandleSongSectionsChanged()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            var sectionData = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Temp);
            for (int i = 0; i < song.Sections.Count; i++)
            {
                sectionData[i] = song.Sections[i].ToUnmanaged();
            }

            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerMidiReloadMsg(sectionData));
        }
    }

    private void HandleSongSettingsChanged()
    {
        NotifyTrackSettingsChanged();
    }

    private void HandleSongEffectsChanged()
    {
        NotifyTrackSettingsChanged();
    }

    private void HandleSongTracksChanged()
    {
        NotifyTrackSettingsChanged();
    }

    private void NotifyTrackSettingsChanged()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            var trackSettings = new NativeArray<AnysongTrackSettings.Unmanaged>(song.Tracks.Count, Allocator.Persistent);
            for (int i = 0; i < song.Tracks.Count; i++)
            {
                trackSettings[i] = song.Tracks[i].ToUnmanaged();
            }

            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsReload(trackSettings));
        }
        else
        {
            Debug.Log("No generator instance");
        }
    }

    public void SetSectionLock(bool state, int lockedSectionIndex)
    {
        _currentLockSectionIndex = lockedSectionIndex;
        _sectionLockState = state;
    }

    public void SetSong(AnysongObject newSong)
    {
        song = newSong;
    }

    public delegate void AudioGeneratedDelegate(float[] samples, int channels);

    public event AudioGeneratedDelegate OnAudioGenerated;
    public static event AudioGeneratedDelegate OnAudioGeneratedStatic;

    private void HandleAudioGeneratedStatic(float[] samples, int channels)
    {
        OnAudioGenerated?.Invoke(samples, channels);
    }

    public void SetPlay(bool state)
    {
        _isPlaying = state;
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerPlaybackMsg(_isPlaying));
        }
    }

    public void SetIntensity(float value)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerIntensityMsg(value));
        }
    }

    float _currentSnapshotValue;

    public void SetSnapshot(float newSnapshotValue)
    {
        if (!Mathf.Approximately(newSnapshotValue, _currentSnapshotValue))
        {
            AnywhenSnapshotBlender.ApplyBlend(song, newSnapshotValue);
            song.RefreshSettings();
            _currentSnapshotValue = newSnapshotValue;
        }
    }

    private void OnDestroy()
    {
        UnregisterSongListeners();
    }

    public void OverrideTrackSettings(AnysongObject sourceSong, int overrideTrackTypeIndex)
    {
        List<AnysongTrackSettings> trackSettingsList = new List<AnysongTrackSettings>();
        for (int i = 0; i < sourceSong.Tracks.Count; i++)
        {
            if (sourceSong.Tracks[i].trackTypeIndex == overrideTrackTypeIndex)
            {
                trackSettingsList.Add(sourceSong.Tracks[i]);
            }
        }

        NativeArray<AnysongTrackSettings.Unmanaged> trackSettings =
            new NativeArray<AnysongTrackSettings.Unmanaged>(trackSettingsList.Count, Allocator.Persistent);

        for (int i = 0; i < trackSettingsList.Count; i++)
        {
            trackSettings[i] = trackSettingsList[i].ToUnmanaged();
        }

        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsReload(trackSettings));
        }
    }

    private void SwapSounds(AnysongObject sourceSong)
    {
        if (!sourceSong)
        {
            return;
        }

        NativeArray<AnysongTrackSettings.Unmanaged> trackSettings =
            new NativeArray<AnysongTrackSettings.Unmanaged>(sourceSong.Tracks.Count, Allocator.Persistent);
        for (int i = 0; i < trackSettings.Length; i++)
        {
            trackSettings[i] = sourceSong.Tracks[i].ToUnmanaged();
        }

        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsReload(trackSettings));
        }
    }

    private void SwapMidi(AnysongObject sourceSong)
    {
        if (!sourceSong)
        {
            return;
        }

        NativeArray<AnysongSection.Unmanaged> newSections =
            new NativeArray<AnysongSection.Unmanaged>(sourceSong.Sections.Count, Allocator.Persistent);

        for (int i = 0; i < newSections.Length; i++)
        {
            newSections[i] = sourceSong.Sections[i].ToUnmanaged();
        }

        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerMidiReloadMsg(newSections));
        }
    }

    public void SetMutedTracks(List<int> mutedTracks)
    {
        NativeArray<int> mutedTrackIndices = new NativeArray<int>(mutedTracks.Count, Allocator.Persistent);
        for (int i = 0; i < mutedTracks.Count; i++)
        {
            mutedTrackIndices[i] = mutedTracks[i];
        }

        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackStateMsg(mutedTrackIndices));
        }
    }


    public enum LoadOptions
    {
        Default,
        OnlyTrackSettings,
        OnlyMidiSettings
    }


    public void Load(AnysongObject currentSong, LoadOptions options = LoadOptions.Default)
    {
        Debug.Log("Loading song: " + currentSong.name + " load options: " + options + " ");

        switch (options)
        {
            case LoadOptions.Default:
                song = currentSong;
                foreach (var track in song.Tracks)
                {
                    foreach (var audioSource in track.AudioSources)
                    {
                        if (audioSource.audioSourceType == AudioSourceSettings.AudioSourceTypes.Sample)
                        {
                            var sampleInstrument = audioSource.sampleSourceSettings.sampleInstrument;
                            if (sampleInstrument && !InstrumentDatabase.IsLoaded(sampleInstrument))
                            {
                                InstrumentDatabase.LoadInstrumentNotes(sampleInstrument);
                            }
                        }
                    }
                }

                break;
            case LoadOptions.OnlyTrackSettings:
                SwapSounds(currentSong);

                break;
            case LoadOptions.OnlyMidiSettings:
                SwapMidi(currentSong);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options, null);
        }
    }

    public bool isFinite => false;
    public bool isRealtime => true;
    public DiscreteTime? length => null;
    public bool SectionLockState => _sectionLockState;

    private NativeArray<int> _sharedStepIndices;
    private NativeArray<int> _sharedPatternIndices;
    private NativeArray<int> _sharedSectionIndices;

    public int GetPlayingSectionIndex()
    {
        if (!_sharedSectionIndices.IsCreated)
            return 0;

        return _sharedSectionIndices[0];
    }

    public int GetPlayingPatternIndexForTrackIndex(int trackIndex)
    {
        if (!_sharedPatternIndices.IsCreated || trackIndex >= _sharedPatternIndices.Length)
        {
            return 0;
        }

        return _sharedPatternIndices[trackIndex];
    }

    public int GetPlaybackStepIndex(int trackIndex)
    {
        if (!_sharedStepIndices.IsCreated || trackIndex >= _sharedStepIndices.Length)
            return 0;

        return _sharedStepIndices[trackIndex];
    }


    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        int trackCount = song != null ? song.Tracks.Count : 0;
        if (_sharedStepIndices.IsCreated) _sharedStepIndices.Dispose();
        _sharedStepIndices = new NativeArray<int>(trackCount, Allocator.Persistent);
        if (_sharedPatternIndices.IsCreated) _sharedPatternIndices.Dispose();
        _sharedPatternIndices = new NativeArray<int>(trackCount, Allocator.Persistent);
        if (_sharedSectionIndices.IsCreated) _sharedSectionIndices.Dispose();
        _sharedSectionIndices = new NativeArray<int>(1, Allocator.Persistent);

        _generatorInstance = Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, song, _sharedStepIndices,
            _sharedPatternIndices,
            _sharedSectionIndices);
        RegisterSongListeners();
        return _generatorInstance;
    }


    private class TriggerTrackStateMsg
    {
        public NativeArray<int> MutedTracks;


        public TriggerTrackStateMsg(NativeArray<int> mutedTracks)
        {
            MutedTracks = mutedTracks;
        }
    }

    private class TriggerPlaybackMsg
    {
        public readonly bool IsPlaying;

        public TriggerPlaybackMsg(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }
    }

    private class TriggerIntensityMsg
    {
        public readonly float Intensity;

        public TriggerIntensityMsg(float intensity)
        {
            Intensity = intensity;
        }
    }

    private class TriggerTrackSettingsReload
    {
        public readonly NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;

        public TriggerTrackSettingsReload(NativeArray<AnysongTrackSettings.Unmanaged> trackSettings)
        {
            TrackSettings = trackSettings;
        }
    }

    private class TriggerMidiReloadMsg
    {
        public readonly NativeArray<AnysongSection.Unmanaged> SectionData;

        public TriggerMidiReloadMsg(NativeArray<AnysongSection.Unmanaged> sectionData)
        {
            SectionData = sectionData;
        }
    }

    private class TriggerNoteClipMsg
    {
        private readonly SimpleNoteEvent _noteEvent;
        private readonly double _scheduledPlayTime;

        public PlaybackEvent PlaybackEvent => new(_noteEvent, _scheduledPlayTime);

        public TriggerNoteClipMsg(SimpleNoteEvent noteEvent, double scheduledPlayTime)
        {
            _noteEvent = noteEvent;
            _scheduledPlayTime = scheduledPlayTime;
        }
    }


    private struct AudioDataEvent
    {
        public const int MaxSamples = 256;
        public int SampleCount;
        public int Channels;
        public unsafe fixed float Samples[MaxSamples];
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<AnysongTrack> _tracks;
        NativeArray<AnysongSection.Unmanaged> _anysongSections;
        private int _sampleRate;
        private AudioDataEvent _audioDataEvent;
        GeneratorInstance.Setup _setup;
        private GeneratorInstance _selfHandle;
        private int _lastSub16Count;
        private NativeArray<int> _stepIndices;
        private NativeArray<int> _patternIndices;
        private NativeArray<int> _sectionIndices;

        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, AnysongObject initialSong,
            NativeArray<int> stepIndices,
            NativeArray<int> patternIndices, NativeArray<int> sectionIndices)
        {
            var processor = new Processor(sampleRate, initialSong, stepIndices, patternIndices, sectionIndices);
            var handle = context.AllocateGenerator(processor, new Control());
            processor._selfHandle = handle;
            return handle;
        }

        public bool isFinite => false;
        public bool isRealtime => true;
        public DiscreteTime? length => null;
        private bool _isPlaying;
        private float _intensity;

        private uint _seed;
        int _currentSectionIndex;
        private int _currentSectionBar;

        Processor(int sampleRate, AnysongObject song, NativeArray<int> stepIndices, NativeArray<int> patternIndices,
            NativeArray<int> sectionIndices)
        {
            _stepIndices = stepIndices;
            _patternIndices = patternIndices;
            _sectionIndices = sectionIndices;
            _seed = 12345;
            _setup = new GeneratorInstance.Setup();
            _selfHandle = default;
            _lastSub16Count = -1;
            _anysongSections = default;
            _isPlaying = false;
            _currentSectionIndex = 0;
            _intensity = 1;
            _sampleRate = sampleRate;
            _currentSectionBar = 0;
            _audioDataEvent = new AudioDataEvent { Channels = 2 }; // Assuming stereo for now
            if (song != null)
            {
                _anysongSections = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Persistent);
                for (int i = 0; i < song.Sections.Count; i++)
                {
                    _anysongSections[i] = song.Sections[i].ToUnmanaged();
                }

                _tracks = new NativeArray<AnysongTrack>(song.Tracks.Count, Allocator.Persistent);

                for (int i = 0; i < _tracks.Length; i++)
                {
                    var trackSettings = song.Tracks[i];
                    var unmanagedSettings = trackSettings.ToUnmanaged();
                    _tracks[i] = new AnysongTrack(sampleRate, unmanagedSettings);
                }
            }
            else
            {
                _tracks = new NativeArray<AnysongTrack>(0, Allocator.Persistent);
            }
        }


        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            foreach (var element in pipe.GetAvailableData(context))
            {
                if (element.TryGetData(out PlaybackStateData data))
                {
                    if (data.IsPlaying)
                    {
                        _currentSectionBar = 0;
                        _currentSectionIndex = 0;
                        _sectionIndices[0] = 0;
                        if (_anysongSections is { IsCreated: true, Length: > 0 })
                        {
                            AnywhenAudioMetronome.Processor.SetBaseProgression(_anysongSections[_currentSectionIndex]
                                .ProgressionSteps);
                        }

                        for (int sectionIndex = 0; sectionIndex < _anysongSections.Length; sectionIndex++)
                        {
                            for (int trackIndex = 0; trackIndex < _anysongSections[sectionIndex].Tracks.Length; trackIndex++)
                            {
                                var section = _anysongSections[sectionIndex];
                                section.Reset();
                                _anysongSections[sectionIndex] = section;

                                var track = section.Tracks[trackIndex];
                                track.Reset();
                                var pattern = track.Patterns[track.CurrentPatternIndex];
                                track.Patterns[track.CurrentPatternIndex] = pattern;
                                section.Tracks[trackIndex] = track;
                                _patternIndices[trackIndex] = track.CurrentPatternIndex;
                            }
                        }
                    }

                    _isPlaying = data.IsPlaying;
                }

                if (element.TryGetData(out IntensityData intensityData))
                {
                    _intensity = intensityData.Intensity;
                }

                if (element.TryGetData(out NewTracksSettingsData newTrackSettings))
                {
                    for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
                    {
                        var thisTrack = _tracks[trackIndex];
                        //foreach (var newTrackSetting in newTrackSettings.TrackSettings)
                        {
                            //if (newTrackSetting.trackTypeIndex == thisTrack.TrackTypeIndex)
                            {
                                //thisTrack.CreateTrack(newTrackSettings.TrackSettings[trackIndex], _sampleRate);
                                thisTrack.UpdateSettings(newTrackSettings.TrackSettings[trackIndex]);
                            }
                        }

                        _tracks[trackIndex] = thisTrack;
                    }

                    newTrackSettings.TrackSettings.Dispose();
                }

                if (element.TryGetData(out SwapMidiData newMidiData))
                {
                    if (_anysongSections.Length != newMidiData.SectionData.Length)
                    {
                        if (_anysongSections.IsCreated)
                            _anysongSections.Dispose();
                        _anysongSections =
                            new NativeArray<AnysongSection.Unmanaged>(newMidiData.SectionData.Length, Allocator.Persistent);
                        for (int i = 0; i < newMidiData.SectionData.Length; i++)
                            _anysongSections[i] = newMidiData.SectionData[i];
                        _currentSectionIndex %= _anysongSections.Length;
                    }

                    for (int sectionIndex = 0; sectionIndex < newMidiData.SectionData.Length; sectionIndex++)
                    {
                        var section = _anysongSections[sectionIndex];
                        var newSection = newMidiData.SectionData[sectionIndex];
                        for (int trackIndex = 0; trackIndex < newSection.Tracks.Length; trackIndex++)
                        {
                            var track = section.Tracks[trackIndex];
                            var newTrack = newSection.Tracks[trackIndex];
                            for (int patternIndex = 0; patternIndex < newTrack.Patterns.Length; patternIndex++)
                            {
                                track.Patterns[patternIndex] = newTrack.Patterns[patternIndex];
                            }

                            var pattern = track.Patterns[track.CurrentPatternIndex];
                            track.Patterns[track.CurrentPatternIndex] = pattern;
                            section.Tracks[trackIndex] = track;
                        }

                        _anysongSections[sectionIndex] = section;
                    }
                }

                if (element.TryGetData(out TrackStateData newTrackStateData))
                {
                    for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
                    {
                        bool isMuted = false;
                        for (int i = 0; i < newTrackStateData.MutedTracks.Length; i++)
                        {
                            if (newTrackStateData.MutedTracks[i] == trackIndex)
                            {
                                isMuted = true;
                            }
                        }

                        var thisTrack = _tracks[trackIndex];
                        thisTrack.IsMute = isMuted;
                        _tracks[trackIndex] = thisTrack;
                    }
                }
            }
        }


        public unsafe GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe,
            ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            uint state = _seed;

            int NextInt(int min, int max)
            {
                if (min >= max) return min;
                state = state * 1103515245 + 12345;
                return min + (int)((state >> 16) % (uint)(max - min));
            }

            double sampleRate = _setup.sampleRate;
            double invSampleRate = 1.0 / sampleRate;
            double dspTime = context.dspTime * invSampleRate;

            int currentSub16Count = AnywhenAudioMetronome.SharedSub16Count.Data;
            if (!_anysongSections.IsCreated) return buffer.frameCount;

            if (_isPlaying && currentSub16Count != _lastSub16Count)
            {
                if (currentSub16Count == 0)
                {
                    int prevIndex = _currentSectionIndex;
                    var section = _anysongSections[prevIndex];

                    section.AdvancePlayingSection();
                    if (section.IsComplete())
                    {
                        _currentSectionBar = 0;
                        section.Reset();
                        _currentSectionIndex = (_currentSectionIndex + 1) % _anysongSections.Length;
                        AnywhenAudioMetronome.Processor.SetBaseProgression(
                            _anysongSections[_currentSectionIndex].ProgressionSteps);
                        _sectionIndices[0] = _currentSectionIndex;
                    }

                    var currentSection = _anysongSections[_currentSectionIndex];
                    if (currentSection.ProgressionSteps.Length > 0)
                    {
                        _currentSectionBar %= currentSection.ProgressionSteps.Length;
                        //AnywhenRuntime.Metronome.SetBaseProgressionStep(currentSection.ProgressionSteps[_currentSectionBar]);
                        _currentSectionBar = (_currentSectionBar + 1) % currentSection.ProgressionSteps.Length;
                    }

                    _anysongSections[prevIndex] = section;
                }

                for (int trackIndex = 0; trackIndex < _anysongSections[_currentSectionIndex].Tracks.Length; trackIndex++)
                {
                    var section = _anysongSections[_currentSectionIndex];
                    var track = section.Tracks[trackIndex];
                    if (currentSub16Count == 0)
                    {
                        track.AdvancePlayingPattern();
                        _patternIndices[trackIndex] = track.CurrentPatternIndex;
                    }

                    var pattern = track.Patterns[track.CurrentPatternIndex];


                    if (_stepIndices.IsCreated && trackIndex < _stepIndices.Length)
                    {
                        _stepIndices[trackIndex] = pattern.GetStepIndex(currentSub16Count);
                    }


                    foreach (var thisNote in pattern.GetCurrentStep(currentSub16Count).StepNotes)
                    {
                        bool chancePass = thisNote.chance * 100 > NextInt(0, 100);
                        bool intensityPass = thisNote.mixWeight > (1 - _intensity);

                        if (chancePass && intensityPass)
                        {
                            var playbackTrack = _tracks[trackIndex];
                            playbackTrack.HandlePlaybackEvent(new PlaybackEvent(thisNote, dspTime));
                            _tracks[trackIndex] = playbackTrack;
                        }
                    }


                    track.Patterns[track.CurrentPatternIndex] = pattern;
                    section.Tracks[trackIndex] = track;
                    _anysongSections[_currentSectionIndex] = section;
                }

                _lastSub16Count = currentSub16Count;
            }

            if (sampleRate <= 0)
                return buffer.frameCount;

            // Clear buffer before mixing
            for (var frame = 0; frame < buffer.frameCount; frame++)
            {
                for (var channel = 0; channel < buffer.channelCount; channel++)
                    buffer[channel, frame] = 0;
            }

            for (int i = 0; i < _tracks.Length; i++)
            {
                var track = _tracks[i];
                for (var frame = 0; frame < buffer.frameCount; frame++)
                {
                    double currentFrameDspTime = dspTime + (frame * invSampleRate);
                    float trackAmp = track.Process(currentFrameDspTime);

                    for (var channel = 0; channel < buffer.channelCount; channel++)
                        buffer[channel, frame] += trackAmp;
                }

                _tracks[i] = track;
            }

            // Capture samples for visualization
            _audioDataEvent.Channels = buffer.channelCount;
            _audioDataEvent.SampleCount = Math.Min(buffer.frameCount, AudioDataEvent.MaxSamples);
            for (int i = 0; i < _audioDataEvent.SampleCount; i++)
            {
                // For oscilloscope, we can just take the first channel or mix them
                _audioDataEvent.Samples[i] = buffer[0, i];
            }

            pipe.SendData(context, _audioDataEvent);

            _seed = state;
            return buffer.frameCount;
        }

        struct Control : GeneratorInstance.IControl<Processor>
        {
            private static float[] _managedSamples;

            public void Configure(ControlContext context, ref Processor generator, in AudioFormat config,
                out GeneratorInstance.Setup setup, ref GeneratorInstance.Properties p)
            {
                generator._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator._setup;
                // Only allocate if not already done
                if (_managedSamples is not { Length: AudioDataEvent.MaxSamples })
                    _managedSamples = new float[AudioDataEvent.MaxSamples];
            }

            public void Dispose(ControlContext context, ref Processor generator)
            {
                if (generator._tracks.IsCreated)
                {
                    for (int i = 0; i < generator._tracks.Length; i++)
                        generator._tracks[i].Dispose();
                    generator._tracks.Dispose();
                }

                if (generator._anysongSections.IsCreated)
                {
                    for (int i = 0; i < generator._anysongSections.Length; i++)
                    {
                        generator._anysongSections[i].Dispose();
                    }

                    generator._anysongSections.Dispose();
                }
            }

            public unsafe void Update(ControlContext context, ProcessorInstance.Pipe pipe)
            {
                foreach (var element in pipe.GetAvailableData(context))
                {
                    if (element.TryGetData(out AudioDataEvent data))
                    {
                        for (int i = 0; i < data.SampleCount; i++)
                        {
                            _managedSamples[i] = data.Samples[i];
                        }

                        OnAudioGeneratedStatic?.Invoke(_managedSamples, data.Channels);
                    }
                }
            }

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe,
                ProcessorInstance.Message message)
            {
                if (message.Is<TriggerNoteClipMsg>())
                {
                    var payload = message.Get<TriggerNoteClipMsg>();
                    pipe.SendData(context, payload.PlaybackEvent);
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerPlaybackMsg>())
                {
                    var payload = message.Get<TriggerPlaybackMsg>();
                    pipe.SendData(context, new PlaybackStateData { IsPlaying = payload.IsPlaying });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerIntensityMsg>())
                {
                    var payload = message.Get<TriggerIntensityMsg>();
                    pipe.SendData(context, new IntensityData { Intensity = payload.Intensity });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerTrackStateMsg>())
                {
                    var payload = message.Get<TriggerTrackStateMsg>();
                    pipe.SendData(context, new TrackStateData { MutedTracks = payload.MutedTracks });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerTrackSettingsReload>())
                {
                    var payload = message.Get<TriggerTrackSettingsReload>();
                    pipe.SendData(context, new NewTracksSettingsData { TrackSettings = payload.TrackSettings });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerMidiReloadMsg>())
                {
                    var payload = message.Get<TriggerMidiReloadMsg>();
                    pipe.SendData(context, new SwapMidiData { SectionData = payload.SectionData });
                    return ProcessorInstance.Response.Handled;
                }

                return ProcessorInstance.Response.Unhandled;
            }
        }
    }


    public struct PlaybackEvent : IEquatable<PlaybackEvent>
    {
        public AnysongPatternNote Note;
        public readonly double ScheduledPlayTime;
        public readonly double ScheduledEndTime;

        public PlaybackEvent(AnysongPatternNote note, double scheduledPlayTime)
        {
            Note = note;
            ScheduledPlayTime = scheduledPlayTime + Note.drift * AnywhenAudioMetronome.Sub16Length;
            ScheduledEndTime = scheduledPlayTime + Note.duration +
                               Note.drift * AnywhenAudioMetronome.Sub16Length;
        }

        public PlaybackEvent(SimpleNoteEvent simpleNoteEvent, double scheduledPlayTime)
        {
            Note = new AnysongPatternNote(simpleNoteEvent.note);
            ScheduledPlayTime = scheduledPlayTime + simpleNoteEvent.drift;
            ScheduledEndTime = ScheduledPlayTime + Note.duration + simpleNoteEvent.drift;
        }

        public bool Equals(PlaybackEvent other)
        {
            return Note.Equals(other.Note) && ScheduledPlayTime.Equals(other.ScheduledPlayTime) &&
                   ScheduledEndTime.Equals(other.ScheduledEndTime);
        }

        public override bool Equals(object obj)
        {
            return obj is PlaybackEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Note, ScheduledPlayTime, ScheduledEndTime);
        }
    }

    private struct PlaybackStateData
    {
        public bool IsPlaying;
    }

    private struct IntensityData
    {
        public float Intensity;
    }

    private struct NewTracksSettingsData
    {
        public NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;
    }

    private struct SwapMidiData
    {
        public NativeArray<AnysongSection.Unmanaged> SectionData;
    }

    private struct TrackStateData
    {
        public NativeArray<int> MutedTracks;
    }
}