using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class AnywhenMenuUtils : MonoBehaviour
    {
        [MenuItem("Anywhen/[DEBUG] Remove audioclips")]
        public static void RemoveAudioClips()
        {
            foreach (var o in Selection.objects)
            {
                Debug.Log(o.name);
                var instrument = o as AnywhenSampleInstrument;
                if (instrument && instrument.clipType == AnywhenSampleInstrument.ClipTypes.NoteClips)
                {
                    
                    foreach (var clip in instrument.audioClips)
                    {
                        var path = AssetDatabase.GetAssetPath(clip);
                        AssetDatabase.DeleteAsset(path);
                        
                    }

                    instrument.audioClips = null;
                    
                }
            }
        }


        [MenuItem("Anywhen/Add Anywhen to scene")]
        public static void AddAnywhen()
        {
            Debug.Log("Adding Anywhen to active scene");


            for (int i = 0; i < SceneManager.GetActiveScene().rootCount; i++)
            {
                var currentAnywhen = SceneManager.GetActiveScene().GetRootGameObjects()[i].GetComponentsInChildren<AnywhenRuntime>();
                foreach (var anywhenRuntime in currentAnywhen)
                {
                    EditorUtility.DisplayDialog("Anywhen detected", "It looks like this scene already contains an instance of Anywhen", "ok");
                    Debug.Log("found anywhen " + anywhenRuntime.name);
                    Selection.activeObject = anywhenRuntime;
                    return;
                }
            }


            var path = GetAssetPath("Prefabs/Anywhen.prefab");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);


            if (prefab)
            {
                var anywhenInstance = PrefabUtility.InstantiatePrefab(prefab);
                AnywhenRuntime rt = anywhenInstance.GetComponent<AnywhenRuntime>();
                rt.Init();
                Selection.activeObject = anywhenInstance;
            }
        }

        /// <summary>
        /// Finds complete path to provided path.
        ///
        /// Note: The more precise you are the less likely there will be "multipath" errors,
        /// but on the other hand if you change any directory name in your path,
        /// you will get "no path found" errors.
        /// </summary>
        /// <param name="assetStaticPath">static unchanging path to the asset (example: Prefabs/Prefab.asset)</param>
        /// <returns>found path, null if not found or found multiple</returns>
        public static string GetAssetPath(string assetStaticPath)
        {
            List<string> foundPaths = new List<string>();
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var fileName = assetStaticPath;
            for (int i = 0; i < allAssetPaths.Length; ++i)
            {
                if (allAssetPaths[i].EndsWith(fileName))
                    foundPaths.Add(allAssetPaths[i]);
            }

            if (foundPaths.Count == 1)
                return foundPaths[0];

            if (foundPaths.Count == 0)
            {
                Debug.LogError($"No path found for asset {assetStaticPath}!");
            }
            else if (foundPaths.Count > 1)
            {
                Debug.LogError($"Multiple paths found for asset {assetStaticPath}, use more precise static path!");

                for (int i = 0; i < foundPaths.Count; i++)
                {
                    string path = foundPaths[i];
                    Debug.LogError($"Path {i + 1}: {path}");
                }
            }

            return null;
        }
    }
}