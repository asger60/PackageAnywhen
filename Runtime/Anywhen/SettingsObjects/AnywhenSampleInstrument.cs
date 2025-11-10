using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenSampleInstrument : AnywhenInstrument
    {
        public enum ClipSelectType
        {
            ScalePitchedNotes,
            RandomVariations,
            UnscaledNotes
        }


        public ClipSelectType clipSelectType;


        //[Serializable]
        //public struct LoopSettings
        //{
        //    public bool enabled;
        //    public int loopStart;
        //    public int loopLength;
        //    public int crossFadeDuration;
        //}

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
                //this.enabled = true;
            }

            public bool IsUnset()
            {
                return attack == 0 && decay == 0 && sustain == 0 && release == 0;
            }

            public void Initialize()
            {
                attack = 0.01f;
                decay = 0.1f;
                sustain = 1f;
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

        public AnywhenNoteClip GetNoteClip(int note)
        {
            var clips = InstrumentDatabase.GetNoteClips(this);
            if (clips == null)
            {
                return null;
            }


            switch (clipSelectType)
            {
                case ClipSelectType.ScalePitchedNotes:
                    note = AnywhenRuntime.Conductor.GetScaledNote(note, clips.Count);
                    
                    if (note >= clips.Count)
                    {
                        AnywhenRuntime.Log("note out of clip range", AnywhenRuntime.DebugMessageType.Warning);
                        return null;
                    }

                    if (note < 0)
                    {
                        Debug.LogWarning("note value is below 0");
                        return null;
                    }

                    return note >= clips.Count ? null : clips[note];

                case ClipSelectType.RandomVariations:
                    return clips[Random.Range(0, clips.Count)];

                case ClipSelectType.UnscaledNotes:
                    return clips[Mathf.Clamp(note, 0, clips.Count)];

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        public void PreviewSound()
        {
            InstrumentDatabase.LoadInstrumentNotes(this);
            AnywhenRuntime.PreviewNoteClip(GetNoteClip(Random.Range(0, 10)));
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
            [FormerlySerializedAs("Guid")] public string guid;
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
                    path = AssetDatabase.GetAssetPath(noteClip)
                };
                clipData.guid = AssetDatabase.AssetPathToGUID(clipData.path);
                clipDatas[i] = clipData;
            }

            EditorUtility.SetDirty(this);
        }

#endif
    }
}