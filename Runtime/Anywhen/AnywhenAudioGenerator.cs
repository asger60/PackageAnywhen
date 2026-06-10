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

    private bool _isPlaying;
    private GeneratorInstance _generatorInstance;

    private void OnEnable()
    {
        OnAudioGeneratedStatic += HandleAudioGeneratedStatic;
        OnMidiEventTriggeredStatic += HandleMidiEventTriggeredStatic;
        OnPlaybackIndicesChangedStatic += HandlePlaybackIndicesChangedStatic;
    }

    private void OnDisable()
    {
        OnAudioGeneratedStatic -= HandleAudioGeneratedStatic;
        OnMidiEventTriggeredStatic -= HandleMidiEventTriggeredStatic;
        OnPlaybackIndicesChangedStatic -= HandlePlaybackIndicesChangedStatic;
    }


    public void HandleSongMidiChanged()
    {
        NotifySongMidiChanged();
    }

    public void HandleSongSectionsChanged()
    {
        NotifySongMidiChanged();
    }

    public void HandleSongSettingsChanged()
    {
        NotifyTrackSettingsChanged();
    }

    public void HandleTrackRebuild(int trackIndex)
    {
        NotifyTrackRebuild(trackIndex);
    }

    private void NotifyTrackRebuild(int trackIndex)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsReload(song.Tracks[trackIndex].ToUnmanaged(), trackIndex));
        }
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

            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsUpdate(trackSettings));
        }
        else
        {
            Debug.Log("No generator instance");
        }
    }

    private void NotifySongMidiChanged()
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            var sectionData = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Persistent);
            for (int i = 0; i < song.Sections.Count; i++)
            {
                sectionData[i] = song.Sections[i].ToUnmanaged();
            }

            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerMidiReloadMsg(sectionData));
        }
    }


    public void SetSong(AnysongObject newSong)
    {
        InstrumentDatabase.LoadAllInstruments(newSong);
        song = Instantiate(newSong);
    }

    public delegate void PlaybackIndicesDelegate(int sectionIndex, int[] patternIndices, int[] stepIndices);

    public event PlaybackIndicesDelegate OnPlaybackIndicesChanged;
    public static event PlaybackIndicesDelegate OnPlaybackIndicesChangedStatic;

    public delegate void AudioGeneratedDelegate(float[] samples, int channels);

    public event AudioGeneratedDelegate OnAudioGenerated;
    public static event AudioGeneratedDelegate OnAudioGeneratedStatic;

    public delegate void MidiTriggeredDelegate(MidiDataEvent[] midiDataEvents);

    public event MidiTriggeredDelegate OnMidiEventTriggered;
    public static event MidiTriggeredDelegate OnMidiEventTriggeredStatic;

    private void HandleAudioGeneratedStatic(float[] samples, int channels)
    {
        OnAudioGenerated?.Invoke(samples, channels);
    }

    private void HandleMidiEventTriggeredStatic(MidiDataEvent[] midiDataEvents)
    {
        OnMidiEventTriggered?.Invoke(midiDataEvents);
    }

    private void HandlePlaybackIndicesChangedStatic(int sectionIndex, int[] patternIndices, int[] stepIndices)
    {
        _managedSectionIndex = sectionIndex;
        for (int i = 0; i < PlaybackIndicesEvent.MaxTracks; i++)
        {
            _managedPatternIndices[i] = patternIndices[i];
            _managedStepIndices[i] = stepIndices[i];
        }

        OnPlaybackIndicesChanged?.Invoke(sectionIndex, patternIndices, stepIndices);
    }

    public void SetPlay(bool state, int startSectionIndex, bool sectionLocked)
    {
        if (state)
        {
            InstrumentDatabase.LoadAllInstruments(song);
            InstrumentDatabase.RefreshUnamangedInstruments();
        }

        _isPlaying = state;
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance,
                new TriggerPlaybackMsg(_isPlaying, startSectionIndex, sectionLocked));
        }
    }


    public void SetSectionLocked(bool sectionLocked, int lockedSectionIndex = -1)
    {
        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new ToggleSectionLockMsg(sectionLocked, lockedSectionIndex));
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

            _currentSnapshotValue = newSnapshotValue;

            if (ControlContext.builtIn.Exists(_generatorInstance))
            {
                var trackSettings = new NativeArray<AnysongTrackSettings.Unmanaged>(song.Tracks.Count, Allocator.Persistent);
                for (int i = 0; i < song.Tracks.Count; i++)
                {
                    trackSettings[i] = song.Tracks[i].ToUnmanaged();
                }

                ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsUpdate(trackSettings));
            }
            else
            {
                Debug.Log("No generator instance");
            }
        }
    }


    public void OverrideTrackSettings(AnysongObject sourceSong, int overrideTrackTypeIndex)
    {
        var sourceTrackSettings = new AnysongTrackSettings();
        int trackIndex = 0;
        foreach (var sourceTrack in sourceSong.Tracks)
        {
            if (sourceTrack.trackTypeIndex == overrideTrackTypeIndex)
            {
                sourceTrackSettings = sourceTrack;
                break;
            }
        }

        for (int i = 0; i < song.Tracks.Count; i++)
        {
            if (song.Tracks[i].trackTypeIndex == overrideTrackTypeIndex)
            {
                song.Tracks[i] = new AnysongTrackSettings();
                song.Tracks[i] = sourceTrackSettings.Clone();
                trackIndex = i;
                break;
            }
        }


        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance,
                new TriggerTrackSettingsReload(sourceTrackSettings.ToUnmanaged(), trackIndex));
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
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerTrackSettingsUpdate(trackSettings));
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
        switch (options)
        {
            case LoadOptions.Default:
                song = currentSong;

                InstrumentDatabase.LoadAllInstruments(currentSong);
                foreach (var track in song.Tracks)
                {
                    foreach (var audioSource in track.AudioSources)
                    {
                        if (audioSource.audioSourceType == AudioSourceSettings.AudioSourceTypes.Sample)
                        {
                            var sampleInstrument = audioSource.sampleSourceSettings.sampleInstrument;
                            if (sampleInstrument && !InstrumentDatabase.IsLoaded(sampleInstrument))
                            {
#if UNITY_EDITOR
                                InstrumentDatabase.LoadInstrumentNotes(sampleInstrument);
#endif
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

    private int _managedSectionIndex;
    private int[] _managedPatternIndices = new int[PlaybackIndicesEvent.MaxTracks];
    private int[] _managedStepIndices = new int[PlaybackIndicesEvent.MaxTracks];

    public int GetPlayingSectionIndex()
    {
        return _managedSectionIndex;
    }

    public int GetPlayingPatternIndexForTrackIndex(int trackIndex)
    {
        if (trackIndex >= _managedPatternIndices.Length)
        {
            return 0;
        }

        return _managedPatternIndices[trackIndex];
    }

    public int GetPlaybackStepIndex(int trackIndex)
    {
        if (trackIndex >= _managedStepIndices.Length)
            return 0;

        return _managedStepIndices[trackIndex];
    }


    public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
        ProcessorInstance.CreationParameters creationParameters)
    {
        _generatorInstance = Processor.Allocate(context, nestedFormat?.sampleRate ?? 48000, song);
        return _generatorInstance;
    }


    private class TriggerTrackStateMsg
    {
        public readonly NativeArray<int> MutedTracks;


        public TriggerTrackStateMsg(NativeArray<int> mutedTracks)
        {
            MutedTracks = mutedTracks;
        }
    }

    private class TriggerPlaybackMsg
    {
        public readonly bool IsPlaying;
        public readonly int StartSectionIndex;
        public readonly bool SectionLocked;

        public TriggerPlaybackMsg(bool isPlaying, int startSectionIndex, bool sectionLocked)
        {
            IsPlaying = isPlaying;
            StartSectionIndex = startSectionIndex;
            SectionLocked = sectionLocked;
        }
    }

    private class ToggleSectionLockMsg
    {
        public readonly bool IsSectionLocked;
        public int SectionLockedIndex;

        public ToggleSectionLockMsg(bool isSectionLocked, int sectionLockedIndex)
        {
            IsSectionLocked = isSectionLocked;
            SectionLockedIndex = sectionLockedIndex;
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

    private class TriggerTrackSettingsUpdate
    {
        public readonly NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;

        public TriggerTrackSettingsUpdate(NativeArray<AnysongTrackSettings.Unmanaged> trackSettings)
        {
            TrackSettings = trackSettings;
        }
    }

    private class TriggerTrackSettingsReload
    {
        public readonly int TrackIndex;
        public readonly AnysongTrackSettings.Unmanaged TrackSettings;

        public TriggerTrackSettingsReload(AnysongTrackSettings.Unmanaged trackSettings, int trackIndex)
        {
            TrackSettings = trackSettings;
            TrackIndex = trackIndex;
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


    private struct PlaybackIndicesEvent
    {
        public const int MaxTracks = 32;
        public int SectionIndex;
        public unsafe fixed int PatternIndices[MaxTracks];
        public unsafe fixed int StepIndices[MaxTracks];
    }

    private struct AudioDataEvent
    {
        public const int MaxSamples = 256;
        public int SampleCount;
        public int Channels;
        public unsafe fixed float Samples[MaxSamples];
    }

    public struct MidiDataEvent
    {
        public int MidiNote;
        public float Velocity;
        public int TrackTypeIndex;
        public float NoteDuration;

        public MidiDataEvent(int midiNote, float velocity, int trackTypeIndex, float noteDuration)
        {
            MidiNote = midiNote;
            Velocity = velocity;
            TrackTypeIndex = trackTypeIndex;
            NoteDuration = noteDuration;
        }

        public bool IsNull()
        {
            return MidiNote == 0 && Velocity == 0 && TrackTypeIndex == 0 && NoteDuration == 0;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<AnysongTrack> _tracks;
        NativeArray<AnysongSection.Unmanaged> _anysongSections;
        private int _sampleRate;
        private AudioDataEvent _audioDataEvent;
        private NativeArray<MidiDataEvent> _midiDataEvents;
        private PlaybackIndicesEvent _playbackIndicesEvent;
        GeneratorInstance.Setup _setup;
        private GeneratorInstance _selfHandle;
        private int _lastSub16Count;
        bool _sectionLocked;
        private NativeArray<float> _mixBuffer;
        const int blockSize = 64;


        public static GeneratorInstance Allocate(ControlContext context, int sampleRate, AnysongObject initialSong)
        {
            InstrumentDatabase.GetLoadedInstrumentsUnmanaged(); // Ensure instruments are loaded on managed side
            var processor = new Processor(sampleRate, initialSong);
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
        int _currentSectionBar;


        Processor(int sampleRate, AnysongObject song)
        {
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
            _sectionLocked = false;
            _audioDataEvent = new AudioDataEvent { Channels = 1 }; // Assuming stereo for now
            _midiDataEvents = new NativeArray<MidiDataEvent>(16, Allocator.Persistent);
            _playbackIndicesEvent = default;
            _mixBuffer = new NativeArray<float>(blockSize, Allocator.Persistent);

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
                    _tracks[i] = new AnysongTrack(sampleRate, unmanagedSettings, blockSize);
                }
            }
            else
            {
                _tracks = new NativeArray<AnysongTrack>(0, Allocator.Persistent);
            }
        }

        private void Dispose()
        {
            for (int i = 0; i < _tracks.Length; i++)
                _tracks[i].Dispose();
            _tracks.Dispose();
            _anysongSections.Dispose();
            _mixBuffer.Dispose();
            _midiDataEvents.Dispose();
            _playbackIndicesEvent = default;
            _audioDataEvent = default;
            _setup = default;
            _selfHandle = default;
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
                        _currentSectionIndex = data.StartSectionIndex;
                        _sectionLocked = data.SectionLocked;

                        if (_anysongSections is { IsCreated: true, Length: > 0 })
                        {
                            AnywhenAudioMetronome.Processor.SetBaseProgression(_anysongSections[_currentSectionIndex].ProgressionSteps);
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
                            }
                        }
                    }

                    _isPlaying = data.IsPlaying;
                }

                if (element.TryGetData(out IntensityData intensityData))
                {
                    _intensity = intensityData.Intensity;
                }

                if (element.TryGetData(out TrackSettingsUpdate settingsUpdate))
                {
                    for (int trackIndex = 0; trackIndex < settingsUpdate.TrackSettings.Length; trackIndex++)
                    {
                        if (trackIndex < _tracks.Length)
                        {
                            var thisTrack = _tracks[trackIndex];
                            thisTrack.UpdateSettings(settingsUpdate.TrackSettings[trackIndex], blockSize);
                            _tracks[trackIndex] = thisTrack;
                        }
                        else
                        {
                            settingsUpdate.TrackSettings[trackIndex].Dispose();
                        }
                    }

                    settingsUpdate.TrackSettings.Dispose();
                }

                if (element.TryGetData(out CreateNewTracksSettingsData newTrackSettings))
                {
                    var thisTrack = _tracks[newTrackSettings.TrackIndex];

                    thisTrack.CreateTrack(newTrackSettings.TrackSettings, _sampleRate, blockSize);
                    thisTrack.UpdateSettings(newTrackSettings.TrackSettings, blockSize);

                    _tracks[newTrackSettings.TrackIndex] = thisTrack;
                }


                if (element.TryGetData(out SwapMidiData newMidiData))
                {
                    if (_anysongSections.Length != newMidiData.SectionData.Length)
                    {
                        if (_anysongSections.IsCreated) _anysongSections.Dispose();

                        _anysongSections =
                            new NativeArray<AnysongSection.Unmanaged>(newMidiData.SectionData.Length, Allocator.Persistent);
                        for (int i = 0; i < newMidiData.SectionData.Length; i++)
                        {
                            _anysongSections[i] = newMidiData.SectionData[i];
                        }

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

                        section.SectionLength = newSection.SectionLength;
                        section.Groove = newSection.Groove;
                        _anysongSections[sectionIndex] = section;
                    }

                    newMidiData.SectionData.Dispose();
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

                if (element.TryGetData(out SectionLockStateData sectionLockStateData))
                {
                    _sectionLocked = sectionLockStateData.SectionLocked;
                    if (sectionLockStateData.LockedSectionIndex != -1)
                    {
                        _currentSectionIndex = sectionLockStateData.LockedSectionIndex;
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

            if (!_anysongSections.IsCreated) return buffer.frameCount;

            if (sampleRate <= 0) return buffer.frameCount;
            int currentEventsCount = 0;
            for (int i = 0; i < _midiDataEvents.Length; i++)
            {
                _midiDataEvents[i] = default;
            }

            for (int blockOffset = 0; blockOffset < buffer.frameCount; blockOffset += blockSize)
            {
                double dspTime = (context.dspTime + (ulong)blockOffset) * invSampleRate;
                int currentSub16Count = AnywhenAudioMetronome.SharedSub16Count.Data;
                var section = _anysongSections[_currentSectionIndex];

                if (_isPlaying && currentSub16Count != _lastSub16Count)
                {
                    if (currentSub16Count == 0)
                    {
                        if (_currentSectionBar >= section.SectionLength && !_sectionLocked)
                        {
                            _currentSectionBar = 0;
                            _currentSectionIndex = (_currentSectionIndex + 1) % _anysongSections.Length;

                            section = _anysongSections[_currentSectionIndex];
                            section.Reset();
                            for (int trackIndex = 0; trackIndex < section.Tracks.Length; trackIndex++)
                            {
                                var track = section.Tracks[trackIndex];
                                track.Reset();
                                section.Tracks[trackIndex] = track;
                            }


                            AnywhenAudioMetronome.Processor.SetBaseProgression(section.ProgressionSteps);
                        }

                        _currentSectionBar++;
                    }

                    _playbackIndicesEvent.SectionIndex = _currentSectionIndex;

                    for (int trackIndex = 0; trackIndex < section.Tracks.Length; trackIndex++)
                    {
                        var track = section.Tracks[trackIndex];
                        if (currentSub16Count == 0)
                        {
                            track.AdvancePlayingPattern();
                        }

                        var pattern = track.Patterns[track.CurrentPatternIndex];

                        if (trackIndex < PlaybackIndicesEvent.MaxTracks)
                        {
                            _playbackIndicesEvent.PatternIndices[trackIndex] = track.CurrentPatternIndex;
                            _playbackIndicesEvent.StepIndices[trackIndex] = pattern.GetStepIndex(currentSub16Count);
                        }

                        foreach (var thisNote in pattern.GetCurrentStep(currentSub16Count).StepNotes)
                        {
                            bool chancePass = thisNote.chance * 100 > NextInt(0, 100);
                            bool intensityPass = thisNote.mixWeight > (1 - _intensity);

                            if (chancePass && intensityPass)
                            {
                                _midiDataEvents[currentEventsCount] =
                                    new MidiDataEvent(thisNote.noteIndex, thisNote.velocity, _tracks[trackIndex].TrackTypeIndex, thisNote.duration);
                                var playbackTrack = _tracks[trackIndex];
                                playbackTrack.HandlePlaybackEvent(
                                    new PlaybackEvent(
                                        thisNote,
                                        _anysongSections[_currentSectionIndex].GetGrooveValue(currentSub16Count),
                                        dspTime)
                                );

                                _tracks[trackIndex] = playbackTrack;
                            }
                        }


                        track.Patterns[track.CurrentPatternIndex] = pattern;
                        section.Tracks[trackIndex] = track;
                        _anysongSections[_currentSectionIndex] = section;
                    }

                    _lastSub16Count = currentSub16Count;
                }


                // Clear buffer before mixing
                for (var frame = 0; frame < _mixBuffer.Length; frame++)
                {
                    _mixBuffer[frame] = 0;
                }

                for (int i = 0; i < _tracks.Length; i++)
                {
                    var track = _tracks[i];
                    track.Process(dspTime, invSampleRate, _mixBuffer, blockSize);
                    _tracks[i] = track;
                }

                _seed = state;
                for (int frame = 0; frame < blockSize; frame++)
                {
                    buffer[0, blockOffset + frame] = _mixBuffer[frame];
                }
            }

            _audioDataEvent.Channels = buffer.channelCount;
            _audioDataEvent.SampleCount = Math.Min(buffer.frameCount, AudioDataEvent.MaxSamples);
            for (int i = 0; i < _audioDataEvent.SampleCount; i++)
            {
                _audioDataEvent.Samples[i] = buffer[0, i];
            }

            pipe.SendData(context, _midiDataEvents);
            pipe.SendData(context, _audioDataEvent);
            pipe.SendData(context, _playbackIndicesEvent);

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
                generator.Dispose();

                if (generator._anysongSections.IsCreated)
                {
                    for (int i = 0; i < generator._anysongSections.Length; i++)
                    {
                        generator._anysongSections[i].Dispose();
                    }

                    generator._anysongSections.Dispose();
                }
            }

            private static int[] _managedPatternIndicesArray = new int[PlaybackIndicesEvent.MaxTracks];
            private static int[] _managedStepIndicesArray = new int[PlaybackIndicesEvent.MaxTracks];

            public unsafe void Update(ControlContext context, ProcessorInstance.Pipe pipe)
            {
                foreach (var element in pipe.GetAvailableData(context))
                {
                    if (element.TryGetData(out NativeArray<MidiDataEvent> midiData))
                    {
                        OnMidiEventTriggeredStatic?.Invoke(midiData.ToArray());
                    }

                    if (element.TryGetData(out AudioDataEvent data))
                    {
                        for (int i = 0; i < data.SampleCount; i++)
                        {
                            _managedSamples[i] = data.Samples[i];
                        }

                        OnAudioGeneratedStatic?.Invoke(_managedSamples, data.Channels);
                    }

                    if (element.TryGetData(out PlaybackIndicesEvent indices))
                    {
                        for (int i = 0; i < PlaybackIndicesEvent.MaxTracks; i++)
                        {
                            _managedPatternIndicesArray[i] = indices.PatternIndices[i];
                            _managedStepIndicesArray[i] = indices.StepIndices[i];
                        }

                        OnPlaybackIndicesChangedStatic?.Invoke(indices.SectionIndex, _managedPatternIndicesArray,
                            _managedStepIndicesArray);
                    }
                }
            }

            public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe,
                ProcessorInstance.Message message)
            {
                if (message.Is<TriggerPlaybackMsg>())
                {
                    var payload = message.Get<TriggerPlaybackMsg>();
                    pipe.SendData(context,
                        new PlaybackStateData
                        {
                            IsPlaying = payload.IsPlaying,
                            StartSectionIndex = payload.StartSectionIndex,
                            SectionLocked = payload.SectionLocked
                        });
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

                if (message.Is<TriggerTrackSettingsUpdate>())
                {
                    var payload = message.Get<TriggerTrackSettingsUpdate>();
                    pipe.SendData(context, new TrackSettingsUpdate { TrackSettings = payload.TrackSettings });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerTrackSettingsReload>())
                {
                    var payload = message.Get<TriggerTrackSettingsReload>();
                    pipe.SendData(context,
                        new CreateNewTracksSettingsData()
                            { TrackSettings = payload.TrackSettings, TrackIndex = payload.TrackIndex });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerMidiReloadMsg>())
                {
                    var payload = message.Get<TriggerMidiReloadMsg>();
                    pipe.SendData(context, new SwapMidiData { SectionData = payload.SectionData });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<ToggleSectionLockMsg>())
                {
                    var payload = message.Get<ToggleSectionLockMsg>();
                    pipe.SendData(context,
                        new SectionLockStateData() { SectionLocked = payload.IsSectionLocked, LockedSectionIndex = payload.SectionLockedIndex });
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

        public PlaybackEvent(AnysongPatternNote note, float grooveValue, double scheduledPlayTime)
        {
            Note = note;
            ScheduledPlayTime = scheduledPlayTime + ((Note.drift + grooveValue) * AnywhenAudioMetronome.Processor.Sub16Length);
            ScheduledEndTime = scheduledPlayTime +
                               Note.duration + ((Note.drift + grooveValue) * AnywhenAudioMetronome.Processor.Sub16Length);
        }


        public bool Equals(PlaybackEvent other)
        {
            return Note.Equals(other.Note) && ScheduledPlayTime.Equals(other.ScheduledPlayTime) &&
                   ScheduledEndTime.Equals(other.ScheduledEndTime);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Note, ScheduledPlayTime, ScheduledEndTime);
        }
    }

    private struct PlaybackStateData
    {
        public bool IsPlaying;
        public int StartSectionIndex;
        public bool SectionLocked;
    }

    private struct IntensityData
    {
        public float Intensity;
    }

    private struct TrackSettingsUpdate
    {
        public NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;
    }

    private struct CreateNewTracksSettingsData
    {
        public int TrackIndex;
        public AnysongTrackSettings.Unmanaged TrackSettings;
    }


    private struct SwapMidiData
    {
        public NativeArray<AnysongSection.Unmanaged> SectionData;
    }

    private struct TrackStateData
    {
        public NativeArray<int> MutedTracks;
    }

    private struct SectionLockStateData
    {
        public bool SectionLocked;
        public int LockedSectionIndex;
    }
}