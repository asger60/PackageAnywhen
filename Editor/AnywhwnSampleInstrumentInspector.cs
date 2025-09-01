using System.Collections.Generic;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

[CustomEditor(typeof(AnywhenSampleInstrument))]
public class AnywhenSampleInstrumentInspector : UnityEditor.Editor
{
    private AnywhenSampleInstrument _anywhenSampleInstrument;
    private VisualElement _dropZone;
    private SerializedProperty _noteClipsProperty;

    private void OnEnable()
    {
        _anywhenSampleInstrument = target as AnywhenSampleInstrument;
        _noteClipsProperty = serializedObject.FindProperty("clipDatas");
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        var previewButton = new Button
        {
            text = "Preview"
        };
        previewButton.clicked += () => { _anywhenSampleInstrument.PreviewSound(); };

        // Create drop zone
        _dropZone = new VisualElement
        {
            style =
            {
                backgroundColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 0.5f)),
                //borderColor = new StyleColor(new Color(0.3f, 0.4f, 0.6f)),
                //borderWidth = new StyleFloat(2f),
                marginTop = 10,
                marginBottom = 10,
                paddingTop = 20,
                paddingBottom = 20,
                paddingLeft = 10,
                paddingRight = 10,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                minHeight = 80
            }
        };

        var dropLabel = new Label("Drop AudioClips here to create NoteClips")
        {
            style =
            {
                unityTextAlign = TextAnchor.MiddleCenter,
                fontSize = 14
            }
        };

        _dropZone.Add(dropLabel);
        _dropZone.RegisterCallback<DragEnterEvent>(OnDragEnter);
        _dropZone.RegisterCallback<DragLeaveEvent>(OnDragLeave);
        _dropZone.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        _dropZone.RegisterCallback<DragPerformEvent>(OnDragPerform);

        // Fill default inspector
        InspectorElement.FillDefaultInspector(inspector, serializedObject, this);
        
        // Add drop zone after the default inspector
        inspector.Add(_dropZone);
        inspector.Add(previewButton);
        
        return inspector;
    }

    private void OnDragEnter(DragEnterEvent evt)
    {
        if (DragAndDrop.objectReferences.Length > 0 && AreAllAudioClips(DragAndDrop.objectReferences))
        {
            _dropZone.style.backgroundColor = new StyleColor(new Color(0.3f, 0.5f, 0.7f, 0.6f));
        }
    }

    private void OnDragLeave(DragLeaveEvent evt)
    {
        _dropZone.style.backgroundColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 0.5f));
    }

    private void OnDragUpdated(DragUpdatedEvent evt)
    {
        if (DragAndDrop.objectReferences.Length > 0 && AreAllAudioClips(DragAndDrop.objectReferences))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        DragAndDrop.AcceptDrag();
        _dropZone.style.backgroundColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 0.5f));

        if (DragAndDrop.objectReferences.Length > 0 && AreAllAudioClips(DragAndDrop.objectReferences))
        {
            AddAudioClipsToInstrument(DragAndDrop.objectReferences);
        }
    }

    private bool AreAllAudioClips(Object[] objects)
    {
        foreach (var obj in objects)
        {
            if (!(obj is AudioClip))
            {
                return false;
            }
        }
        return true;
    }

    private void AddAudioClipsToInstrument(Object[] audioClips)
{
    serializedObject.Update();
    
    List<AnywhenNoteClip> createdNoteClips = new List<AnywhenNoteClip>();
    
    // Create NoteClips for each AudioClip
    foreach (var obj in audioClips)
    {
        if (obj is AudioClip audioClip)
        {
            // Get the directory of the audio clip
            string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
            string directory = Path.GetDirectoryName(audioClipPath);
            
            // Create the new NoteClip in the same directory as the audio clip
            AnywhenNoteClip newNoteClip = AnywhenNoteClip.CreateNewAsset(audioClip, directory);
            createdNoteClips.Add(newNoteClip);
        }
    }
    
    // Update the clipDatas array with the created note clips
    int currentSize = _noteClipsProperty.arraySize;
    _noteClipsProperty.arraySize = currentSize + createdNoteClips.Count;
    
    for (int i = 0; i < createdNoteClips.Count; i++)
    {
        var element = _noteClipsProperty.GetArrayElementAtIndex(currentSize + i);
        var clipPath = AssetDatabase.GetAssetPath(createdNoteClips[i]);
        
        // Update the ClipData properties
        var nameProperty = element.FindPropertyRelative("name");
        var pathProperty = element.FindPropertyRelative("path");
        var guidProperty = element.FindPropertyRelative("guid");
        
        if (nameProperty != null) nameProperty.stringValue = createdNoteClips[i].name;
        if (pathProperty != null) pathProperty.stringValue = clipPath;
        if (guidProperty != null) guidProperty.stringValue = AssetDatabase.AssetPathToGUID(clipPath);
    }
    
    serializedObject.ApplyModifiedProperties();
    EditorUtility.SetDirty(_anywhenSampleInstrument);
    AssetDatabase.SaveAssets();
    
    // Show confirmation message
    EditorUtility.DisplayDialog("NoteClips Created", 
        $"{createdNoteClips.Count} NoteClip(s) created and added to the instrument.", 
        "OK");
}
}