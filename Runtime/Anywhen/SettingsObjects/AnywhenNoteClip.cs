using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    public class AnywhenNoteClip : AnywhenSettingsBase
    {
        //public AudioClip sourceClip;

        public float[] clipSamples;
        public int frequency;
        public int channels;
        public AnywhenSampleInstrument.EnvelopeSettings envelopeSettings;
        public AnywhenSampleInstrument.LoopSettings loopSettings;
        internal void ReadAudioClip(AudioClip audioClip)
        {
            clipSamples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(clipSamples, 0);
            frequency = audioClip.frequency;
            channels = audioClip.channels;
         //   sourceClip = audioClip;
        }
        #if UNITY_EDITOR

        private static void CreateNewAsset()
        {
            var activeObject = Selection.activeObject as AudioClip;
            if (activeObject == null) return;

            var newNoteClip = CreateInstance<AnywhenNoteClip>();

            var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(activeObject));
            var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(activeObject));
            //var extension = Path.GetExtension(AssetDatabase.GetAssetPath(activeObject));

            var fullPath = directory + "/" + fileName;
            AssetDatabase.CreateAsset(newNoteClip, fullPath + ".asset");

            //var clip = Instantiate<AudioClip>(activeObject);

            //extension = ".asset";
            Debug.Log(fullPath + ".asset");


            newNoteClip.ReadAudioClip(activeObject);
            
            EditorUtility.SetDirty(newNoteClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [ContextMenu("Print path")]
        void PrintPath()
        {
            Debug.Log(AssetDatabase.GetAssetPath(this));
        }

        #endif
    }
}