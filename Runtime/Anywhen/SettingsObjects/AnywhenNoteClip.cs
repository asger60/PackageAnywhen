using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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

        private void ReadAudioClip(AudioClip thisSource)
        {
            clipSamples = new float[thisSource.samples * thisSource.channels];
            thisSource.GetData(clipSamples, 0);
            frequency = thisSource.frequency;
            channels = thisSource.channels;
            this.sourceClip = thisSource;
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


            //var clipClone = Instantiate(activeObject);
            AudioClip newAudioClip =
                AudioClip.Create(activeObject.name, activeObject.samples, activeObject.channels, activeObject.frequency,
                    false);
            float[] copyData = new float[activeObject.samples * activeObject.channels];
            activeObject.GetData(copyData, 0);
            newAudioClip.SetData(copyData, 0);
            Debug.Log(newAudioClip.samples * newAudioClip.channels);
            newAudioClip.LoadAudioData();


            //AssetDatabase.DeleteAsset(fullPath + "aaaa" + extension);
            // AssetDatabase.CreateAsset(newAudioClip, "Assets/test.asset");
            AssetDatabase.AddObjectToAsset(newAudioClip, newNoteClip);
            newAudioClip.SetData(copyData, 0);

            //AssetDatabase.ImportAsset(fullPath + ".asset", ImportAssetOptions.Default);

            newNoteClip.ReadAudioClip(newAudioClip);
            newAudioClip.LoadAudioData();
            Debug.Log(newAudioClip.samples * newAudioClip.channels);

            //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(activeObject));
            EditorUtility.SetDirty(newNoteClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newNoteClip;
            //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(activeObject));
        }

        private static AudioClip CloneAudioClip(AudioClip audioClip)
        {
            AudioClip newAudioClip =
                AudioClip.Create(audioClip.name, audioClip.samples, audioClip.channels, audioClip.frequency, false);
            float[] copyData = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(copyData, 0);
            newAudioClip.SetData(copyData, 0);
            Debug.Log(newAudioClip.samples * newAudioClip.channels);
            return newAudioClip;
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