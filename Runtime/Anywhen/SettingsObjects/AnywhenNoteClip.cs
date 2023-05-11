using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New instrument object", menuName = "Anywhen/AudioObjects/NoteClip")]
    public class AnywhenNoteClip : AnywhenSettingsBase
    {
        public AudioClip audioClip;
        public int loopStart;
        public int loopLength;


        [ContextMenu(" nest audioCLip")]
        void NestAudioClip()
        {
            var originalClip = audioClip;
            var originalPath = AssetDatabase.GetAssetPath(originalClip);
            //var originalFolder = Path.GetDirectoryName(originalPath);
            //Debug.Log(originalFolder);
            
            var newPath = "Assets/" + originalClip.name + Path.GetExtension(originalPath);
            AssetDatabase.CopyAsset(originalPath, newPath);
            var clipCopy = AssetDatabase.LoadAssetAtPath<AudioClip>(newPath);
            
            clipCopy.name = originalClip.name + "_AudioClip";
            
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), originalClip.name);
            clipCopy = CloneAudioClip(originalClip, originalClip.name + "-AudioClip");
            AssetDatabase.AddObjectToAsset((clipCopy), AssetDatabase.GetAssetPath(this));
            this.audioClip = clipCopy;
            //AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(originalClip));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private AudioClip CloneAudioClip(AudioClip sourceClip, string newName)
        {
            AudioClip newAudioClip = AudioClip.Create(newName, sourceClip.samples, sourceClip.channels, sourceClip.frequency, false);
            float[] copyData = new float[sourceClip.samples * sourceClip.channels];
            sourceClip.GetData(copyData, 0);
            newAudioClip.SetData(copyData, 0);
            return newAudioClip;
        }
    }
}