using System.IO;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    public class AnywhenNoteClip : AnywhenSettingsBase
    {
        public enum ClipType
        {
            Note,
            Percussion
        }
        
        //public AudioClip sourceClip;
        [HideInInspector] public float[] clipSamples;
        public int frequency;
        public int channels;

        [SerializeField] private ClipType clipType;
        public ClipType Type
        {
            get => clipType;
            set => clipType = value;
        }

        [SerializeField] private int noteIndex;
        public int NoteIndex
        {
            get => noteIndex;
            set => noteIndex = value;
        }
        
        //public AnywhenSampleInstrument.EnvelopeSettings envelopeSettings;
        //public AnywhenSampleInstrument.LoopSettings loopSettings;


#if UNITY_EDITOR
        [MenuItem("Assets/Anywhen/Set Note Index from Filename")]
        private static void SetNoteIndexFromFilename()
        {
            var guids = AssetDatabase.FindAssets("t:AnywhenNoteClip");
            int count = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var noteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(path);
                if (noteClip != null )
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(fileName)) continue;

                    string leadingDigits = "";
                    int i = 0;
                    while (i < fileName.Length && char.IsDigit(fileName[i]))
                    {
                        leadingDigits += fileName[i];
                        i++;
                    }

                    if (!string.IsNullOrEmpty(leadingDigits))
                    {
                        noteClip.NoteIndex = int.Parse(leadingDigits);
                        EditorUtility.SetDirty(noteClip);
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Set NoteIndex for {count} AnywhenNoteClip assets.");
            }
            else
            {
                Debug.Log("No AnywhenNoteClip assets were updated.");
            }
        }

        private void ReadAudioClip(AudioClip audioClip)
        {
            clipSamples = new float[audioClip.samples * audioClip.channels];

            audioClip.GetData(clipSamples, 0);

            Debug.Log(audioClip.name + " samples: " + clipSamples.Length);

            frequency = audioClip.frequency;
            channels = audioClip.channels;
            //   sourceClip = audioClip;
        }

        [MenuItem("Assets/Create/Anywhen/Create instrument from audio clips")]
        private static void CreateNoteClips()
        {
            if (Selection.objects.Length == 0)
            {
                EditorUtility.DisplayDialog("Nothing selected", "You must select some audio clips for this to work", "Ok");
                return;
            }

            bool noClipsSelected = true;
            foreach (var o in Selection.objects)
            {
                if (o is AudioClip)
                {
                    noClipsSelected = false;
                    break;
                }
            }

            if (noClipsSelected)
            {
                EditorUtility.DisplayDialog("Nothing selected", "You must select some audio clips for this to work", "Ok");
                return;
            }

            List<AnywhenNoteClip> newNoteClips = new List<AnywhenNoteClip>();
            var iniitialDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.objects[0]));
            var path = EditorUtility.SaveFilePanel(
                "Save new instrument",
                iniitialDir,
                Selection.objects[0].name + ".asset",
                "asset");

            path = GetRelativePath(path);
            Debug.Log("dir: " + Path.GetDirectoryName(path) + " path " + path);

            foreach (var o in Selection.objects)
            {
                newNoteClips.Add(CreateNewAsset(o as AudioClip, Path.GetDirectoryName(path)));
            }

            var newInstrument = CreateInstance<AnywhenSampleInstrument>();
            newInstrument.LinkClips(newNoteClips.ToArray());
            newInstrument.clipSelectType = AnywhenSampleInstrument.ClipSelectType.RandomVariations;

            AssetDatabase.CreateAsset(newInstrument, AssetDatabase.GenerateUniqueAssetPath(path));


            if (EditorUtility.DisplayDialog("Instrument created", "Do you want to delete the original audioclips?", "Yes",
                    "No, but I promise to do it myself"))
            {
                foreach (var o in Selection.objects)
                {
                    if (o is AudioClip)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(o));
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = newInstrument;
        }

        public static AnywhenNoteClip CreateNewAsset(AudioClip audioClip, string directory)
        {
            var newNoteClip = CreateInstance<AnywhenNoteClip>();
            var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(audioClip));
            var fullPath = directory + "/" + fileName + ".asset";
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(newNoteClip, fullPath);
            newNoteClip.ReadAudioClip(audioClip);
            EditorUtility.SetDirty(newNoteClip);
            AssetDatabase.SaveAssets();
            return newNoteClip;
        }

        public static string GetRelativePath(string absolutePath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..");
            return absolutePath.Replace(projectPath + Path.DirectorySeparatorChar, "");
        }

#endif
    }
}