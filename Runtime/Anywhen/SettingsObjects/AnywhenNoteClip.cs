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
        //public AudioClip sourceClip;

        public float[] clipSamples;
        public int frequency;
        public int channels;

        //public AnywhenSampleInstrument.EnvelopeSettings envelopeSettings;

        //public AnywhenSampleInstrument.LoopSettings loopSettings;
#if UNITY_EDITOR
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

        private static AnywhenNoteClip CreateNewAsset(AudioClip audioClip, string directory)
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