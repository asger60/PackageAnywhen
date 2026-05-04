using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
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
        if (song != null)
        {
            song.RemoveListeners();
        }
    }

    private void OnDisable()
    {
        if (_sharedStepIndices.IsCreated) _sharedStepIndices.Dispose();
        if (_sharedPatternIndices.IsCreated) _sharedPatternIndices.Dispose();
        if (_sharedSectionIndices.IsCreated) _sharedSectionIndices.Dispose();
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
        song?.RemoveListeners();
    }

    public void SwapTrackInstrument(AnywhenSampleInstrument newInstrment, int trackTypeIndex)
    {
        NativeArray<AnysongTrackSettings.Unmanaged> trackSettings =
            new NativeArray<AnysongTrackSettings.Unmanaged>(song.Tracks.Count, Allocator.Persistent);

        for (int i = 0; i < song.Tracks.Count; i++)
        {
            trackSettings[i] = song.Tracks[i].ToUnmanaged();
            var trackSettingsUnmanaged = trackSettings[i];
            if (trackSettingsUnmanaged.trackTypeIndex == trackTypeIndex)
            {
                trackSettingsUnmanaged.instrument = newInstrment.ToUnmanaged();
            }

            trackSettings[i] = trackSettingsUnmanaged;
        }

        if (ControlContext.builtIn.Exists(_generatorInstance))
        {
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerSoundsReloadMsg(trackSettings));
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
            ControlContext.builtIn.SendMessage(_generatorInstance, new TriggerSoundsReloadMsg(trackSettings));
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
        OnlyTrackSounds,
        OnlyMidiSettings
    }


    public void Load(AnysongObject currentSong, LoadOptions options = LoadOptions.Default)
    {
        Debug.Log("Loading song: " + currentSong.name);

        switch (options)
        {
            case LoadOptions.Default:
                song = currentSong;
                foreach (var track in song.Tracks)
                {
                    if (track.instrument is AnywhenSampleInstrument sampleInstrument)
                    {
                        if (!InstrumentDatabase.IsLoaded(sampleInstrument))
                        {
                            InstrumentDatabase.LoadInstrumentNotes(sampleInstrument);
                        }
                    }
                }

                break;
            case LoadOptions.OnlyTrackSounds:
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
            Debug.Log("returning 0");
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
        return _generatorInstance;
    }


    public class TriggerTrackStateMsg
    {
        public NativeArray<int> MutedTracks;


        public TriggerTrackStateMsg(NativeArray<int> mutedTracks)
        {
            MutedTracks = mutedTracks;
        }
    }

    public class TriggerPlaybackMsg
    {
        public readonly bool IsPlaying;

        public TriggerPlaybackMsg(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }
    }

    public class TriggerIntensityMsg
    {
        public float intensity;

        public TriggerIntensityMsg(float intensity)
        {
            this.intensity = intensity;
        }
    }

    public class TriggerSoundsReloadMsg
    {
        public readonly NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;

        public TriggerSoundsReloadMsg(NativeArray<AnysongTrackSettings.Unmanaged> trackSettings)
        {
            TrackSettings = trackSettings;
        }
    }

    public class TriggerMidiReloadMsg
    {
        public readonly NativeArray<AnysongSection.Unmanaged> SectionData;

        public TriggerMidiReloadMsg(NativeArray<AnysongSection.Unmanaged> sectionData)
        {
            SectionData = sectionData;
        }
    }

    public class TriggerNoteClipMsg
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


    [BurstCompile(CompileSynchronously = true)]
    public struct Processor : GeneratorInstance.IRealtime
    {
        NativeArray<AnysongTrack> _tracks;
        NativeArray<AnysongSection.Unmanaged> _anysongSections;
        private int _sampleRate;
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

        private float _currentSnapshotValue;
        private uint _seed;
        int _currentSectionIndex;

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
            _currentSnapshotValue = 0;
            _sampleRate = sampleRate;

            if (song != null)
            {
                _anysongSections = new NativeArray<AnysongSection.Unmanaged>(song.Sections.Count, Allocator.Persistent);
                for (int i = 0; i < song.Sections.Count; i++)
                {
                    _anysongSections[i] = song.Sections[i].ToUnmanaged();
                }


                var sectionsRef = _anysongSections;
                Action<int, int, int> midiListener = (sectionIndex, trackIndex, patternIndex) =>
                {
                    if (!sectionsRef.IsCreated) return;

                    var section = sectionsRef[sectionIndex];
                    var track = section.Tracks[trackIndex];

                    // Preserve internal indices of all patterns in the track
                    var internalIndices = new int[track.Patterns.Length];
                    for (int i = 0; i < track.Patterns.Length; i++)
                    {
                        internalIndices[i] = track.Patterns[i].internalIndex;
                    }

                    // Properly dispose the old NativeArray to avoid memory leaks
                    if (track.Patterns.IsCreated)
                    {
                        for (int i = 0; i < track.Patterns.Length; i++)
                        {
                            if (track.Patterns[i].triggerChances.IsCreated) track.Patterns[i].triggerChances.Dispose();
                            if (track.Patterns[i].steps.IsCreated) track.Patterns[i].steps.Dispose();
                        }

                        track.Patterns.Dispose();
                    }

                    // Create new patterns from the song data
                    track.Patterns = song.Sections[sectionIndex].tracks[trackIndex].ToUnmanaged().Patterns;

                    // Restore internal indices for all patterns
                    for (int i = 0; i < track.Patterns.Length; i++)
                    {
                        var p = track.Patterns[i];
                        if (i < internalIndices.Length)
                        {
                            p.internalIndex = internalIndices[i];
                        }

                        track.Patterns[i] = p;
                    }

                    // Update the CurrentPattern reference in the track
                    track.CurrentPattern = track.Patterns[track.CurrentPatternIndex];

                    section.Tracks[trackIndex] = track;
                    sectionsRef[sectionIndex] = section;
                };
                song.OnSongMidiChanged += midiListener;

                _tracks = new NativeArray<AnysongTrack>(song.Tracks.Count, Allocator.Persistent);

                for (int i = 0; i < _tracks.Length; i++)
                {
                    var trackSettings = song.Tracks[i];
                    if (trackSettings.instrument is AnywhenSampleInstrument)
                    {
                        var unmanagedSettings = trackSettings.ToUnmanaged();
                        _tracks[i] = new AnysongTrack(sampleRate, unmanagedSettings);

                        int capturedIndex = i;
                        var tracksRef = _tracks;
                        Action settingsListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnValuesChanged(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };
                        Action effectsListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnEffectsRebuild(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };

                        Action tracksListener = () =>
                        {
                            if (!tracksRef.IsCreated) return;
                            var t = tracksRef[capturedIndex];
                            t.OnTracksRebuild(song.Tracks[capturedIndex].ToUnmanaged());
                            tracksRef[capturedIndex] = t;
                        };

                        song.OnSongSettingsChanged += settingsListener;
                        song.OnSongEffectsChanged += effectsListener;
                        song.OnSongTracksChanged += tracksListener;
                    }
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
                    _isPlaying = data.IsPlaying;
                    if (_isPlaying)
                    {
                        _currentSectionIndex = 0;
                        _sectionIndices[0] = 0;
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
                                pattern.SyncToMetronome(AnywhenAudioMetronome.SharedSub16Count.Data);
                                track.Patterns[track.CurrentPatternIndex] = pattern;
                                section.Tracks[trackIndex] = track;
                                _patternIndices[trackIndex] = track.CurrentPatternIndex;
                            }
                        }
                    }
                }

                if (element.TryGetData(out IntensityData intensityData))
                {
                    _intensity = intensityData.Intensity;
                }

                if (element.TryGetData(out SwapSoundsData snapshotData))
                {
                    foreach (var trackSettings in snapshotData.TrackSettings)
                    {
                        for (int trackIndex = 0; trackIndex < _tracks.Length; trackIndex++)
                        {
                            var thisTrack = _tracks[trackIndex];
                            if (trackSettings.trackTypeIndex == thisTrack.TrackTypeIndex)
                            {
                                thisTrack.SwapInstrument(trackSettings.instrument);
                            }

                            _tracks[trackIndex] = thisTrack;
                        }
                    }

                    snapshotData.TrackSettings.Dispose();
                }

                if (element.TryGetData(out SwapMidiData newMidiData))
                {
                    for (int sectionIndex = 0; sectionIndex < newMidiData.SectionData.Length; sectionIndex++)
                    {
                        var section = _anysongSections[sectionIndex];
                        var newSection = newMidiData.SectionData[sectionIndex];
                        for (int trackIndex = 0; trackIndex < newSection.Tracks.Length; trackIndex++)
                        {
                            var track = section.Tracks[trackIndex];
                            var newTrack = newSection.Tracks[trackIndex];
                            for (int patternIndex = 0;
                                 patternIndex < newTrack.Patterns.Length;
                                 patternIndex++)
                            {
                                track.Patterns[patternIndex] = newTrack.Patterns[patternIndex];
                            }

                            var pattern = track.Patterns[track.CurrentPatternIndex];
                            pattern.SyncToMetronome(AnywhenAudioMetronome.SharedSub16Count.Data);
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


        public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer,
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

            if (_isPlaying && currentSub16Count != _lastSub16Count)
            {
                if (currentSub16Count == 0)
                {
                    int prevIndex = _currentSectionIndex;
                    var section = _anysongSections[prevIndex];

                    section.AdvancePlayingSection();
                    if (section.IsComplete())
                    {
                        section.Reset();
                        _currentSectionIndex = (_currentSectionIndex + 1) % _anysongSections.Length;
                        _sectionIndices[0] = _currentSectionIndex;
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
                        _stepIndices[trackIndex] = pattern.internalIndex;
                    }


                    foreach (var thisNote in pattern.GetCurrentStep().StepNotes)
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


                    pattern.AdvancePlayingStep();
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

            _seed = state;
            return buffer.frameCount;
        }

        struct Control : GeneratorInstance.IControl<Processor>
        {
            public void Configure(ControlContext context, ref Processor generator, in AudioFormat config,
                out GeneratorInstance.Setup setup, ref GeneratorInstance.Properties p)
            {
                generator._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, config.sampleRate);
                setup = generator._setup;
            }

            public void Dispose(ControlContext context, ref Processor generator)
            {
                if (generator._tracks.IsCreated)
                {
                    for (int i = 0; i < generator._tracks.Length; i++)
                        generator._tracks[i].Dispose();
                    generator._tracks.Dispose();
                }
            }

            public void Update(ControlContext context, ProcessorInstance.Pipe pipe)
            {
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
                    pipe.SendData(context, new IntensityData { Intensity = payload.intensity });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerTrackStateMsg>())
                {
                    var payload = message.Get<TriggerTrackStateMsg>();
                    pipe.SendData(context, new TrackStateData { MutedTracks = payload.MutedTracks });
                    return ProcessorInstance.Response.Handled;
                }

                if (message.Is<TriggerSoundsReloadMsg>())
                {
                    var payload = message.Get<TriggerSoundsReloadMsg>();
                    pipe.SendData(context, new SwapSoundsData { TrackSettings = payload.TrackSettings });
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

    public struct PlaybackStateData
    {
        public bool IsPlaying;
    }

    public struct IntensityData
    {
        public float Intensity;
    }

    public struct SwapSoundsData
    {
        public NativeArray<AnysongTrackSettings.Unmanaged> TrackSettings;
    }

    public struct SwapMidiData
    {
        public NativeArray<AnysongSection.Unmanaged> SectionData;
    }

    public struct TrackStateData
    {
        public NativeArray<int> MutedTracks;
    }
}