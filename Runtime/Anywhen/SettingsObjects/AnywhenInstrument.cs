using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/InstrumentObject")]
    public class AnywhenInstrument : AnywhenSettingsBase
    {
        public AudioClip[] audioClips;
        public AnywhenNoteClip[] noteClips;
        [Range(0, 1f)] public float volume = 1;
        public float stopDuration = 0.1f;

        public enum InstrumentType
        {
            OneShotShort = 0,
            OneShotLong = 1,
            Sustained = 2
        }

        public InstrumentType instrumentType;

        public enum ClipSelectType
        {
            PitchedNotes,
            RandomVariations
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
        }

        public LoopSettings loopSettings;


        [Serializable]
        public struct EnvelopeSettings
        {
            public bool enabled;
            public int attack;
            public int decay;
            public float sustain;
            public int release;

        }

        public EnvelopeSettings envelopeSettings;

        public AnywhenNoteClip GetNoteClip(int note)
        {
            if (noteClips.Length == 0)
            {
                return null;
            }

            switch (clipSelectType)
            {
                case ClipSelectType.PitchedNotes:
                    note = AnywhenConductor.Instance.GetScaledNote(note);
                    if (note >= noteClips.Length)
                    {
                        Debug.LogWarning("note out of clip range");
                        return null;
                    }

                    return note >= noteClips.Length ? null : noteClips[note];

                case ClipSelectType.RandomVariations:
                    return noteClips[Random.Range(0, audioClips.Length)];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public AudioClip GetAudioClip(int note)
        {
            if (audioClips.Length == 0) return null;
            switch (clipSelectType)
            {
                case ClipSelectType.PitchedNotes:
                    note = AnywhenConductor.Instance.GetScaledNote(note);
                    if (note >= audioClips.Length) Debug.LogWarning("note out of clip range");
                    return note >= audioClips.Length ? null : audioClips[note];

                case ClipSelectType.RandomVariations:
                    return audioClips[Random.Range(0, audioClips.Length)];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
                Debug.Log(fullPath + ".asset");
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
    }
}