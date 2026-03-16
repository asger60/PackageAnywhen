using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenSampleInstrument : AnywhenInstrument
    {
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
            new MidiDrumMapping { Name = "Crash", MidiNote = 49 },
        };

        private readonly System.Random _random = new();

        public enum ClipSelectType
        {
            ScalePitchedNotes,
            RandomVariations,
            UnscaledNotes,
            Percussion
        }


        public ClipSelectType clipSelectType;


        [Serializable]
        public struct PitchLFOSettings
        {
            public bool enabled;
            [Range(0.01f, 10)] public float frequency;
            [Range(0, 1)] public float amplitude;
            public bool retrigger;

            public PitchLFOSettings(float frequency, float amplitude, bool retrigger) : this()
            {
                this.frequency = frequency;
                this.amplitude = amplitude;
                this.retrigger = retrigger;
                enabled = false;
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
        public struct EnvelopeSettings
        {
            //public bool enabled;
            [Range(0, 2f)] public float attack;
            [Range(0, 1f)] public float decay;
            [Range(0, 1f)] public float sustain;
            [Range(0, 3f)] public float release;

            public EnvelopeSettings(float attack, float decay, float sustain, float release) : this()
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

        [Range(0, 1f)] public float volume = 1;
        [SerializeField] private int originalTempo = 100;
        [SerializeField] private bool tempoControlPitch;
        public bool TempoControlPitch => tempoControlPitch;

        public float GetPitchFromTempo(float tempo)
        {
            return tempo / originalTempo;
        }

        public struct AnywhenNoteClipPlaybackSettings
        {
            public AnywhenNoteClip noteClip;
            public float clipPitch;
            public float clipVolume;

            public AnywhenNoteClipPlaybackSettings(AnywhenNoteClip noteClip, float clipPitch, float clipVolume)
            {
                this.noteClip = noteClip;
                this.clipPitch = clipPitch;
                this.clipVolume = clipVolume;
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

                    int bestDistance = int.MaxValue;
                    int unsignedDistance = 0;
                    foreach (var noteClip in clips)
                    {
                        var thisDist = Mathf.Abs(noteClip.NoteIndex - note);
                        if (thisDist <= bestDistance)
                        {
                            bestDistance = thisDist;
                            unsignedDistance = note - noteClip.NoteIndex;
                        }
                    }

                    List<AnywhenNoteClip> clipsList = new List<AnywhenNoteClip>();
                    foreach (var noteClip in clips)
                    {
                        if (noteClip.NoteIndex == note - unsignedDistance)
                        {
                            clipsList.Add(noteClip);
                        }
                    }

                    float pitch = 1;
                    if (bestDistance > 0)
                    {
                        pitch = Mathf.Pow(2, unsignedDistance / 12f);
                    }

                    lock (_random)
                    {
                        if (clipsList.Count == 0) return new AnywhenNoteClipPlaybackSettings();
                        return new AnywhenNoteClipPlaybackSettings(clipsList[_random.Next(0, clipsList.Count)], pitch, volume);
                    }


                case ClipSelectType.RandomVariations:
                    lock (_random)
                    {
                        return new AnywhenNoteClipPlaybackSettings(clips[_random.Next(0, clips.Count)], 1, volume);
                    }

                case ClipSelectType.UnscaledNotes:
                    Debug.LogWarning("Unscaled notes are not supported anymore.");
                    lock (_random)
                    {
                        return new AnywhenNoteClipPlaybackSettings(clips[Mathf.Clamp(note, 0, clips.Count)], 1, volume);
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