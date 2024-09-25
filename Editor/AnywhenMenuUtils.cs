using Anywhen;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class AnywhenMenuUtils : MonoBehaviour
    {
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


            string[] interactionGUIDs = AssetDatabase.FindAssets("Anywhen", new[] { "Assets/PackageAnywhen/Samples/Prefabs" });
            GameObject someGameObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(interactionGUIDs[0])) as GameObject;
            Selection.activeObject = PrefabUtility.InstantiatePrefab(someGameObject);
        }
    }
}