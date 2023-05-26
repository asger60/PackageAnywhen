using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    public class AnywhenNoteClip : AnywhenSettingsBase
    {
        public AudioClip sourceClip;

        public float[] clipSamples;
        public int frequency;
        public int channels;
        public AnywhenInstrument.EnvelopeSettings envelopeSettings;
        public AnywhenInstrument.LoopSettings loopSettings;
        internal void ReadAudioClip(AudioClip audioClip)
        {
            clipSamples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(clipSamples, 0);
            frequency = audioClip.frequency;
            channels = audioClip.channels;
            sourceClip = audioClip;
        }

        [MenuItem("Assets/Anywhen/Convert to NoteClip", false, 1)]
        private static void CreateNewAsset()
        {
            var activeObject = Selection.activeObject as AudioClip;
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
        }

        [ContextMenu("Print path")]
        void PrintPath()
        {
            Debug.Log(AssetDatabase.GetAssetPath(this));
        }

        [ContextMenu("add clip")]
        void AddClip()
        {
            AssetDatabase.AddObjectToAsset(Instantiate(sourceClip), AssetDatabase.GetAssetPath(this));
        }
    }
}