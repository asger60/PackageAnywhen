using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenSampleInstrument : AnywhenInstrument
    {
        [SerializeField] private int seed;
        private readonly System.Random _random = new System.Random();

        public struct MidiDrumMapping
        {
            public string Name;
            public int MidiNote;
        }

        public static MidiDrumMapping[] MidiDrumMappings = new MidiDrumMapping[]
        {
            new MidiDrumMapping { Name = "Kick1", MidiNote = 36 },
            new MidiDrumMapping { Name = "Kick2", MidiNote = 35 },
            new MidiDrumMapping { Name = "Click", MidiNote = 37 },
            new MidiDrumMapping { Name = "Snare1", MidiNote = 38 },
            new MidiDrumMapping { Name = "Clap", MidiNote = 39 },
            new MidiDrumMapping { Name = "Snare2", MidiNote = 40 },
            new MidiDrumMapping { Name = "Tom1", MidiNote = 41 },
            new MidiDrumMapping { Name = "HH1", MidiNote = 42 },
            new MidiDrumMapping { Name = "Tom2", MidiNote = 43 },
            new MidiDrumMapping { Name = "HH2", MidiNote = 44 },
            new MidiDrumMapping { Name = "Tom3", MidiNote = 45 },
            new MidiDrumMapping { Name = "OpenHH", MidiNote = 46 },
            new MidiDrumMapping { Name = "Cowbell", MidiNote = 47 },
            new MidiDrumMapping { Name = "Crash", MidiNote = 48 },
        };


        public enum ClipSelectType
        {
            ScalePitchedNotes,
            RandomVariations,
            Percussion
        }


        [SerializeField] public ClipSelectType clipSelectType = ClipSelectType.ScalePitchedNotes;




        [Range(0, 1f)] public float volume = 1;
        [SerializeField] private int originalTempo = 100;
        [SerializeField] private bool tempoControlPitch;
        public bool TempoControlPitch => tempoControlPitch;

        public new struct Unmanaged : IEquatable<Unmanaged>
        {
            public AnywhenInstrument.Unmanaged baseData;
            public ClipSelectType clipSelectType;
            public float volume;
            public int originalTempo;
            public bool tempoControlPitch;
            public uint seed;

            public bool Equals(Unmanaged other)
            {
                return baseData.Equals(other.baseData) &&
                       clipSelectType == other.clipSelectType &&
                       Mathf.Approximately(volume, other.volume) &&
                       originalTempo == other.originalTempo &&
                       tempoControlPitch == other.tempoControlPitch &&
                       seed == other.seed;
            }

            public override bool Equals(object obj)
            {
                return obj is Unmanaged other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(baseData);
                hashCode.Add((int)clipSelectType);
                hashCode.Add(volume);
                hashCode.Add(originalTempo);
                hashCode.Add(tempoControlPitch);
                hashCode.Add(seed);
                return hashCode.ToHashCode();
            }

            public static bool operator ==(Unmanaged left, Unmanaged right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Unmanaged left, Unmanaged right)
            {
                return !left.Equals(right);
            }

            public AnywhenNoteClipPlaybackSettings GetNoteClipSettings(int note)
            {
                var clips = InstrumentDatabase.GetNoteClips(this);
                if (!clips.IsCreated)
                {
                    Debug.LogWarning("no clip found for instrument");
                    return new AnywhenNoteClipPlaybackSettings();
                }

                uint state = seed;

                int NextInt(int min, int max)
                {
                    if (min >= max) return min;
                    state = state * 1103515245 + 12345;
                    return min + (int)((state >> 16) % (uint)(max - min));
                }

                NextInt(0, 1);

                AnywhenNoteClipPlaybackSettings settings;
                switch (clipSelectType)
                {
                    case ClipSelectType.ScalePitchedNotes:
                        note = AnywhenRuntime.Conductor.GetScaledNote(note);

                        int bestDistance = int.MaxValue;
                        int matchingCount = 0;
                        AnywhenNoteClip.Unmanaged selectedClip = default;

                        foreach (var noteClip in clips)
                        {
                            var thisDist = noteClip.noteIndex - note;
                            if (thisDist < 0) thisDist = -thisDist;

                            if (thisDist < bestDistance)
                            {
                                bestDistance = thisDist;
                                matchingCount = 1;
                                selectedClip = noteClip;
                            }
                            else if (thisDist == bestDistance)
                            {
                                matchingCount++;
                                if (NextInt(0, matchingCount) == 0)
                                {
                                    selectedClip = noteClip;
                                }
                            }
                        }

                        float pitch = 1;
                        if (matchingCount > 0 && bestDistance > 0)
                        {
                            pitch = Mathf.Pow(2, (note - selectedClip.noteIndex) * (1f / 12f));
                        }

                        settings = matchingCount == 0
                            ? new AnywhenNoteClipPlaybackSettings()
                            : new AnywhenNoteClipPlaybackSettings(selectedClip, pitch, volume);
                        
                        break;


                    case ClipSelectType.RandomVariations:
                        int index = NextInt(0, clips.Length);
                        settings = new AnywhenNoteClipPlaybackSettings(clips[index], 1, volume);
                        break;


                    case ClipSelectType.Percussion:
                        int count = 0;
                        AnywhenNoteClip.Unmanaged resultClip = default;
                        foreach (var noteClip in clips)
                        {
                            if (noteClip.noteIndex == note)
                            {
                                count++;
                                if (NextInt(0, count) == 0)
                                {
                                    resultClip = noteClip;
                                }
                            }
                        }

                        settings = count == 0
                            ? new AnywhenNoteClipPlaybackSettings()
                            : new AnywhenNoteClipPlaybackSettings(resultClip, 1, volume);

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                seed = state;
                return settings;
            }
        }

        [FormerlySerializedAs("envelope")] [SerializeField] private AudioEnvelopeSettings audioEnvelope = new AudioEnvelopeSettings(0.01f, 0.1f, 0.5f, 0.1f);

        public new Unmanaged ToUnmanaged()
        {
            lock (_random)
            {
                return new Unmanaged
                {
                    baseData = base.ToUnmanaged(),
                    clipSelectType = clipSelectType,
                    volume = volume,
                    originalTempo = originalTempo,
                    tempoControlPitch = tempoControlPitch,
                    seed = (uint)_random.Next(1, int.MaxValue)
                };
            }
        }

        public float GetPitchFromTempo(float tempo)
        {
            return tempo / originalTempo;
        }

        public struct AnywhenNoteClipPlaybackSettings
        {
            public AnywhenNoteClip noteClip;
            public AnywhenNoteClip.Unmanaged NoteClipUnmanaged;
            public float clipPitch;

            public AnywhenNoteClipPlaybackSettings(AnywhenNoteClip noteClip, float clipPitch, float clipVolume)
            {
                this.noteClip = noteClip;
                this.NoteClipUnmanaged = noteClip.ToUnmanaged();
                this.clipPitch = clipPitch;
            }

            public AnywhenNoteClipPlaybackSettings(AnywhenNoteClip.Unmanaged noteClip, float clipPitch, float clipVolume)
            {
                this.noteClip = null;
                this.NoteClipUnmanaged = noteClip;
                this.clipPitch = clipPitch;
            }
        }

        public AnywhenNoteClipPlaybackSettings GetNoteClip(int note)
        {
            var clips = InstrumentDatabase.GetNoteClips(this);
            if (clips == null)
            {
                return new AnywhenNoteClipPlaybackSettings();
            }


            switch (clipSelectType)
            {
                case ClipSelectType.ScalePitchedNotes:
                    note = AnywhenRuntime.Conductor.GetScaledNote(note);

                    int bestDist = int.MaxValue;
                    int bestNoteIndex = 0;

                    foreach (var noteClip in clips)
                    {
                        var thisDist = noteClip.NoteIndex - note;
                        if (thisDist < 0) thisDist = -thisDist;

                        if (thisDist < bestDist)
                        {
                            bestDist = thisDist;
                            bestNoteIndex = noteClip.NoteIndex;
                        }
                    }

                    int count = 0;
                    AnywhenNoteClip resultClip = null;
                    foreach (var noteClip in clips)
                    {
                        if (noteClip.NoteIndex == bestNoteIndex)
                        {
                            count++;
                            if (count == 1)
                            {
                                resultClip = noteClip;
                            }
                            else
                            {
                                lock (_random)
                                {
                                    if (_random.Next(0, count) == 0)
                                    {
                                        resultClip = noteClip;
                                    }
                                }
                            }
                        }
                    }

                    float p = 1;
                    if (bestDist > 0 && resultClip != null)
                    {
                        p = Mathf.Pow(2, (note - resultClip.NoteIndex) * (1f / 12f));
                    }

                    if (resultClip == null) return new AnywhenNoteClipPlaybackSettings();
                    return new AnywhenNoteClipPlaybackSettings(resultClip, p, volume);


                case ClipSelectType.RandomVariations:
                    lock (_random)
                    {
                        return new AnywhenNoteClipPlaybackSettings(clips[_random.Next(0, clips.Count)], 1, volume);
                    }


                case ClipSelectType.Percussion:
                    List<AnywhenNoteClip> percussionClips = new List<AnywhenNoteClip>();
                    foreach (var noteClip in clips)
                    {
                        if (noteClip.NoteIndex == note)
                        {
                            percussionClips.Add(noteClip);
                        }
                    }

                    if (percussionClips.Count == 0) return new AnywhenNoteClipPlaybackSettings();

                    lock (_random)
                    {
                        return new AnywhenNoteClipPlaybackSettings(percussionClips[_random.Next(0, percussionClips.Count)], 1,
                            volume);
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        public void PreviewSound()
        {
            InstrumentDatabase.LoadInstrumentNotes(this);
            AnywhenRuntime.PreviewNoteClip(GetNoteClip(UnityEngine.Random.Range(0, 10)));
        }


        public List<AnywhenNoteClip> LoadClips()
        {
            List<AnywhenNoteClip> loadedClips = new List<AnywhenNoteClip>();

            bool isInPackage = false;
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            if (packageInfo != null)
            {
                isInPackage = true;
            }

            foreach (var clipData in clipDatas)
            {
                var path = clipData.path;
                if (isInPackage)
                {
                    var pathDirs = path.Split("/");
                    List<string> pathDirList = new List<string>();
                    pathDirList.AddRange(pathDirs);
                    pathDirList.RemoveAt(0);
                    pathDirList.RemoveAt(0);
                    path = "Packages/com.floppyclub.anywhen/";
                    for (var i = 0; i < pathDirList.Count; i++)
                    {
                        var dirName = pathDirList[i];
                        path += dirName;
                        if (i != pathDirList.Count - 1)
                            path += "/";
                    }
                }


                var clip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(AssetDatabase.GUIDToAssetPath(clipData.guid));

                loadedClips.Add(clip);
            }

            return loadedClips;
        }


        [Serializable]
        public struct ClipData
        {
            public string name;
            public string path;
            public string guid;
            public AnywhenNoteClip.ClipType clipType;
            public int noteIndex;
        }

        public ClipData[] clipDatas;

        [ContextMenu("UnlinkClips")]
        public void LinkClips(AnywhenNoteClip[] noteClips)
        {
            if (noteClips.Length == 0) return;
            clipDatas = new ClipData[noteClips.Length];
            for (var i = 0; i < noteClips.Length; i++)
            {
                var noteClip = noteClips[i];
                var clipData = new ClipData
                {
                    name = noteClip.name,
                    path = AssetDatabase.GetAssetPath(noteClip),
                    clipType = noteClip.Type,
                    noteIndex = noteClip.NoteIndex
                };
                clipData.guid = AssetDatabase.AssetPathToGUID(clipData.path);
                clipDatas[i] = clipData;
            }

            EditorUtility.SetDirty(this);
        }

#endif
    }
}

[Serializable]
public struct AudioLFOSettings
{
    [Range(0.01f, 10)] public float frequency;
    [Range(0, 1)] public float amplitude;
    
    public AudioLFOSettings(float frequency, float amplitude) : this()
    {
        this.frequency = frequency;
        this.amplitude = amplitude;

    }

    public bool IsUnset()
    {
        return frequency == 0 && amplitude == 0;
    }

    public void Initialize()
    {
        frequency = 1;
        amplitude = 1;
    }
}

[Serializable]
public struct AudioEnvelopeSettings
{
    //public bool enabled;
    [Range(0, 2f)] public float attack;
    [Range(0, 1f)] public float decay;
    [Range(0, 1f)] public float sustain;
    [Range(0, 3f)] public float release;

    public AudioEnvelopeSettings(float attack, float decay, float sustain, float release) : this()
    {
        this.attack = attack;
        this.decay = decay;
        this.sustain = sustain;
        this.release = release;
    }

    public bool IsUnset()
    {
        return attack == 0 && decay == 0 && sustain == 0 && release == 0;
    }

    public void Initialize()
    {
        attack = 0.01f;
        decay = 0.1f;
        sustain = 0.5f;
        release = 0.1f;
    }
}