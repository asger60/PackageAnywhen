using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenSampleInstrument : AnywhenInstrument
    {
        public AudioClip[] audioClips;
        public AnywhenNoteClip[] noteClips;
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
            if (noteClips.Length == 0)
            {
                return null;
            }

            switch (clipSelectType)
            {
                case ClipSelectType.ScalePitchedNotes:
                    note = AnywhenRuntime.Conductor.GetScaledNote(note);
                    if (note >= noteClips.Length)
                    {
                        AnywhenRuntime.Log("note out of clip range", AnywhenRuntime.DebugMessageType.Warning);
                        return null;
                    }

                    if (note < 0)
                    {
                        Debug.LogWarning("note value is below 0");
                        return null;
                    }
                    return note >= noteClips.Length ? null : noteClips[note];

                case ClipSelectType.RandomVariations:
                    return noteClips[Random.Range(0, noteClips.Length)];

                case ClipSelectType.UnscaledNotes:
                    return noteClips[Mathf.Clamp(note, 0, noteClips.Length)];

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public AudioClip GetAudioClip(int note)
        {
            if (audioClips.Length == 0) return null;
            switch (clipSelectType)
            {
                case ClipSelectType.ScalePitchedNotes:
                    note = AnywhenRuntime.Conductor.GetScaledNote(note);
                    if (note >= audioClips.Length) AnywhenRuntime.Log("note out of clip range", AnywhenRuntime.DebugMessageType.Warning);
                    return note >= audioClips.Length ? null : audioClips[note];

                case ClipSelectType.RandomVariations:
                    return audioClips[Random.Range(0, audioClips.Length)];
                case ClipSelectType.UnscaledNotes:
                    return audioClips[Mathf.Clamp(note, 0, audioClips.Length)];

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#if UNITY_EDITOR
        [ContextMenu("ConvertToNoteClips")]
        void ConvertToNoteClips()
        {
            List<AnywhenNoteClip> newNoteClips = new List<AnywhenNoteClip>();

            foreach (var audioClip in audioClips)
            {
                var activeObject = audioClip;
                if (activeObject == null) return;

                var newNoteClip = CreateInstance<AnywhenNoteClip>();

                var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(activeObject));
                var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(activeObject));
                var extension = Path.GetExtension(AssetDatabase.GetAssetPath(activeObject));

                var fullPath = directory + "/" + fileName;
                AssetDatabase.CreateAsset(newNoteClip, fullPath + ".asset");

                var clip = Instantiate<AudioClip>(activeObject);

                extension = ".asset";
                AnywhenRuntime.Log(fullPath + ".asset");
                var newClipPath = "Assets/" + clip.name + extension;
                //AssetDatabase.CreateAsset(clip, newClipPath);
                //AssetDatabase.ImportAsset(newClipPath);
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
                //AssetDatabase.AddObjectToAsset(clip, fullPath + ".asset");

                newNoteClip.ReadAudioClip(activeObject);


                //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(activeObject));
                EditorUtility.SetDirty(newNoteClip);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //Selection.activeObject = newNoteClip;
                newNoteClips.Add(newNoteClip);
            }

            noteClips = newNoteClips.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}