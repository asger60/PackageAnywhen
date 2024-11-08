using System.IO;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;

namespace Anywhen.Editor
{
    public class AudioInstrumentCreator : UnityEditor.Editor
    {
        [MenuItem("Assets/Create/Anywhen/Instrument Object")]
        public static void CreateInstrument()
        {
            AnywhenSampleInstrument asset = CreateInstance<AnywhenSampleInstrument>();

            var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.objects[0]));
            Debug.Log("Create new InstrumentObject at path: " + path);
            AssetDatabase.CreateAsset(asset, path + "/New InstrumentObject.asset");
            AssetDatabase.SaveAssets();
            //asset.audioClips = new AudioClip[Selection.objects.Length];
            //for (int i = 0; i < Selection.objects.Length; i++)
            //{
            //    var o = Selection.objects[i];
            //    asset.audioClips[i] = o as AudioClip;
            //}
            
            Selection.activeObject = asset;
            EditorUtility.FocusProjectWindow();

        }
    }
}
