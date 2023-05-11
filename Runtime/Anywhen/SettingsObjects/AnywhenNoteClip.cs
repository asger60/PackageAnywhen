using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen.SettingsObjects
{
    public class AnywhenNoteClip : AnywhenSettingsBase
    {
        [FormerlySerializedAs("testAudioClip")]
        public AudioClip sourceClip;

        public int loopStart;
        public int loopLength;
        public float[] clipSamples;
        public int frequency;
        public int channels;

        private void ReadAudioClip(AudioClip audioClip)
        {
            var originalClip = audioClip;
            clipSamples = new float[audioClip.samples * audioClip.channels];
            originalClip.GetData(clipSamples, 0);
            frequency = originalClip.frequency;
            channels = originalClip.channels;
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
            ProjectWindowUtil.CreateAsset(newNoteClip, fileName + ".asset");

            var fullPath = directory + "/" + fileName;

            var clip = Instantiate<AudioClip>(activeObject);

            extension = ".asset";
            Debug.Log(fullPath + ".asset");
            var newClipPath = "Assets/" + clip.name + extension;
            AssetDatabase.CreateAsset(clip, newClipPath);
            AssetDatabase.ImportAsset(newClipPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.AddObjectToAsset(clip, fullPath + ".asset");

            newNoteClip.ReadAudioClip(clip);


            //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(activeObject));
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