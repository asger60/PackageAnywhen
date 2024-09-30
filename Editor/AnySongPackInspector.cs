using System.Collections.Generic;
using Anywhen.Composing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnyTrackPackObject))]
public class AnySongPackInspector : UnityEditor.Editor
{
    private AnyTrackPackObject _trackPackObject;

    private void OnEnable()
    {
        _trackPackObject = target as AnyTrackPackObject;
    }

    public static AsyncOperationHandle<IList<AnysongObject>> LoadSongs(AnyTrackPackObject packObject)
    {
        var loadSongs = Addressables.LoadAssetsAsync<AnysongObject>(packObject.AssetLabelReference,
            o =>
            {
                Debug.Log("loaded: " + o.name);
            });
        
        return loadSongs;
    }




    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();
            
        var button = new Button
        {
            text = "Load"
        };
        button.clicked += () =>
        {
            Debug.Log("load");
            LoadSongs(_trackPackObject);
        };

        InspectorElement.FillDefaultInspector(inspector, serializedObject, this);

        inspector.Add(button);
        return inspector;
    }
}