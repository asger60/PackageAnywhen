using System.Collections.Generic;
using Anywhen.Composing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnysongPackObject))]
public class AnySongPackInspector : UnityEditor.Editor
{
    private AnysongPackObject _trackPackObject;

    private void OnEnable()
    {
        _trackPackObject = target as AnysongPackObject;
    }




    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        //var button = new Button
        //{
        //    text = "Load"
        //};
        //button.clicked += () =>
        //{
        //    Debug.Log("load");
        //    LoadSongs(_trackPackObject);
        //};

        InspectorElement.FillDefaultInspector(inspector, serializedObject, this);

        //inspector.Add(button);
        return inspector;
    }
}