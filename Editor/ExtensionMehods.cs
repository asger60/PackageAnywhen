using UnityEditor;
using UnityEngine;

namespace UnitySynth.Runtime.AudioSystem.Editor
{
    public static class ExtensionMethods
    {
        public static T AddElement<T>(this ScriptableObject scriptableObject, SerializedProperty listProperty,
            string name = "Element", HideFlags hideFlags = HideFlags.None) where T : ScriptableObject
        {
            if (!listProperty.isArray)
                throw new System.Exception("\"listProperty\" is not a List.");

            T element = ScriptableObject.CreateInstance<T>();

            element.name = name;
            element.hideFlags = hideFlags;

            string scriptableObjectPath = AssetDatabase.GetAssetPath(scriptableObject);

            AssetDatabase.AddObjectToAsset(element, scriptableObjectPath);
            //


            listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
            SerializedProperty lastElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
            lastElement.objectReferenceValue = element;
            return element;
        }

        public static void RemoveElement<T>(this ScriptableObject scriptableObject,
            SerializedProperty listProperty) where T : ScriptableObject
        {
            if (!listProperty.isArray)
                throw new System.Exception("\"listProperty\" is not a List.");


            if (listProperty.arraySize == 0)
                return;

            int index = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                if ((ScriptableObject) listProperty.GetArrayElementAtIndex(i).objectReferenceValue ==
                    scriptableObject)
                {
                    index = i;
                    break;
                }
            }


            SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(index);
            Object.DestroyImmediate(scriptableObject, true);


            elementProperty.objectReferenceValue = null;
            listProperty.DeleteArrayElementAtIndex(index);
            

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}