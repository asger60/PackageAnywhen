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
        //public AudioClip[] audioClips;
        //private AnywhenNoteClip[] _noteClips;
        public float stopDuration = 0.1f;

        public enum ClipSelectType
        {
            ScalePitchedNotes,
            RandomVariations,
            UnscaledNotes
        }

        public enum ClipTypes
        {
            AudioClips,
            NoteClips
        }

        public ClipSelectType clipSelectType;

        public ClipTypes clipType;

        [Serializable]
        public struct LoopSettings
        {
            public bool enabled;
            public int loopStart;
            public int loopLength;
            public int crossFadeDuration;
        }


        [Serializable]
        public struct EnvelopeSettings
        {
            public bool enabled;
            [Range(0, 10f)] public float attack;
            [Range(0, 5f)] public float decay;
            [Range(0, 1f)] public float sustain;
            [Range(0, 10f)] public float release;

            public EnvelopeSettings(float attack, float decay, float sustain, float release) : this()
            {
                this.attack = attack;
                this.decay = decay;
                this.sustain = sustain;
                this.release = release;
            }
        }

        public LoopSettings loopSettings;
        [Range(0, 1f)] public float volume = 1;


        public AnywhenNoteClip GetNoteClip(int note)
        {
            var clips = InstrumentDatabase.GetNoteClips(this);
            if (clips == null)
            {
                return null;
            }
            //if (_noteClips == null || _noteClips.Length == 0)
            //{
            //    LoadClips();
            //    return null;
            //}

            switch (clipSelectType)
            {
                case ClipSelectType.ScalePitchedNotes:
                    note = AnywhenRuntime.Conductor.GetScaledNote(note);
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

        //public AudioClip GetAudioClip(int note)
        //{
        //    if (audioClips.Length == 0) return null;
        //    switch (clipSelectType)
        //    {
        //        case ClipSelectType.ScalePitchedNotes:
        //            note = AnywhenRuntime.Conductor.GetScaledNote(note);
        //            if (note >= audioClips.Length)
        //                AnywhenRuntime.Log("note out of clip range", AnywhenRuntime.DebugMessageType.Warning);
        //            return note >= audioClips.Length ? null : audioClips[note];
//
        //        case ClipSelectType.RandomVariations:
        //            return audioClips[Random.Range(0, audioClips.Length)];
        //        case ClipSelectType.UnscaledNotes:
        //            return audioClips[Mathf.Clamp(note, 0, audioClips.Length)];
//
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }
        //}
#if UNITY_EDITOR
        //[ContextMenu("ConvertToNoteClips")]
        //void ConvertToNoteClips()
        //{
        //    List<AnywhenNoteClip> newNoteClips = new List<AnywhenNoteClip>();
//
        //    foreach (var audioClip in audioClips)
        //    {
        //        var activeObject = audioClip;
        //        if (activeObject == null) return;
//
        //        var newNoteClip = CreateInstance<AnywhenNoteClip>();
//
        //        var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(activeObject));
        //        var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(activeObject));
        //        var extension = Path.GetExtension(AssetDatabase.GetAssetPath(activeObject));
//
        //        var fullPath = directory + "/" + fileName;
        //        AssetDatabase.CreateAsset(newNoteClip, fullPath + ".asset");
//
        //        var clip = Instantiate<AudioClip>(activeObject);
//
        //        extension = ".asset";
        //        AnywhenRuntime.Log(fullPath + ".asset");
        //        var newClipPath = "Assets/" + clip.name + extension;
        //        //AssetDatabase.CreateAsset(clip, newClipPath);
        //        //AssetDatabase.ImportAsset(newClipPath);
        //        //AssetDatabase.SaveAssets();
        //        //AssetDatabase.Refresh();
        //        //AssetDatabase.AddObjectToAsset(clip, fullPath + ".asset");
//
        //        newNoteClip.ReadAudioClip(activeObject);
//
//
        //        //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(activeObject));
        //        EditorUtility.SetDirty(newNoteClip);
        //        AssetDatabase.SaveAssets();
        //        AssetDatabase.Refresh();
        //        //Selection.activeObject = newNoteClip;
        //        newNoteClips.Add(newNoteClip);
        //    }
//
        //    _noteClips = newNoteClips.ToArray();
        //    AssetDatabase.SaveAssets();
        //    AssetDatabase.Refresh();
        //}

        public void PreviewSound()
        {
            var clips = LoadClips();
            AnywhenRuntime.ClipNoteClipPreviewer.PlayClip(this, clips[0]);
        }

        public void DeleteAudioCLips()
        {
            //foreach (var audioClip in audioClips)
            //{
            //    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(audioClip));
            //}
//
            //audioClips = null;
            //clipType = ClipTypes.NoteClips;
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

            foreach (var clipString in clipDatas)
            {
                var path = clipString.path;
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

                //Debug.Log("loading at path: " + path);


                var clip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(path);
                //Debug.Log(clip);
                
                loadedClips.Add(clip);
            }

            return loadedClips;
            //_noteClips = new AnywhenNoteClip[loadedClips.Count];
            //_noteClips = loadedClips.ToArray();
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
        public void UnlinkClips()
        {
            //if (_noteClips.Length == 0) return;
            //clipDatas = new ClipData[_noteClips.Length];
            //for (var i = 0; i < _noteClips.Length; i++)
            //{
            //    var noteClip = _noteClips[i];
            //    var clipData = new ClipData
            //    {
            //        name = noteClip.name,
            //        path = AssetDatabase.GetAssetPath(noteClip)
            //    };
            //    clipData.guid = AssetDatabase.AssetPathToGUID(clipData.path);
            //    clipDatas[i] = clipData;
            //}
//
            //_noteClips = null;
            //EditorUtility.SetDirty(this);
        }

#endif
    }
}