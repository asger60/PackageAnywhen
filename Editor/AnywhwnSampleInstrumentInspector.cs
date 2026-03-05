using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

[CustomPropertyDrawer(typeof(AnywhenSampleInstrument.ClipData))]
public class AnywhenClipDataDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        // Standard fields
        var nameField = new PropertyField(property.FindPropertyRelative("name"));
        var pathField = new PropertyField(property.FindPropertyRelative("path"));
        var guidField = new PropertyField(property.FindPropertyRelative("guid"));
        var clipTypeField = new PropertyField(property.FindPropertyRelative("clipType"));

        container.Add(nameField);
        container.Add(pathField);
        container.Add(guidField);
        container.Add(clipTypeField);

        // Preview Button
        var previewButton = new Button
        {
            text = "PLAY",
            style =
            {
                marginTop = 5,
                backgroundColor = new StyleColor(new Color(0.25f, 0.45f, 0.25f, 1f)),
                color = new StyleColor(Color.white)
            }
        };
        previewButton.clicked += () =>
        {
            var guid = property.FindPropertyRelative("guid").stringValue;
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var noteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(assetPath);
            if (noteClip != null)
            {
                AnywhenRuntime.PreviewNoteClip(noteClip);
            }
        };
        container.Add(previewButton);

        // Percussion Buttons Section
        var percussionButtons = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap,
                marginTop = 5,
                marginBottom = 5
            }
        };


        List<Button> buttons = new List<Button>();

        var indexProp = property.FindPropertyRelative("noteIndex");
        var clipTypeProp = property.FindPropertyRelative("clipType");

        // Sync from asset
        var initialGuid = property.FindPropertyRelative("guid").stringValue;
        var initialAssetPath = AssetDatabase.GUIDToAssetPath(initialGuid);
        var initialNoteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(initialAssetPath);
        if (initialNoteClip != null)
        {
            bool modified = false;
            if (indexProp.intValue != initialNoteClip.NoteIndex)
            {
                indexProp.intValue = initialNoteClip.NoteIndex;
                modified = true;
            }

            if (clipTypeProp.enumValueIndex != (int)initialNoteClip.Type)
            {
                clipTypeProp.enumValueIndex = (int)initialNoteClip.Type;
                modified = true;
            }

            if (modified)
            {
                indexProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        void UpdateButtonStyles()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var guid = property.FindPropertyRelative("guid").stringValue;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var noteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(assetPath);

                if (AnywhenSampleInstrument.MidiDrumMappings[i].MidiNote == noteClip.NoteIndex)
                {
                    buttons[i].style.backgroundColor = new StyleColor(new Color(0.3f, 0.5f, 0.8f, 1f));
                    buttons[i].style.color = new StyleColor(Color.white);
                }
                else
                {
                    buttons[i].style.backgroundColor = new StyleColor(StyleKeyword.Null);
                    buttons[i].style.color = new StyleColor(StyleKeyword.Null);
                }
            }
        }

        for (int i = 0; i < AnywhenSampleInstrument.MidiDrumMappings.Length; i++)
        {
            int index = AnywhenSampleInstrument.MidiDrumMappings[i].MidiNote;
            var button = new Button
            {
                text = AnywhenSampleInstrument.MidiDrumMappings[i].Name,
                style =
                {
                    fontSize = 10,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginRight = 2,
                    marginBottom = 2
                }
            };
            button.clicked += () =>
            {
                indexProp.intValue = index;
                indexProp.serializedObject.ApplyModifiedProperties();
                UpdateButtonStyles();

                // Also update the asset
                var guid = property.FindPropertyRelative("guid").stringValue;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var noteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(assetPath);
                if (noteClip != null)
                {
                    Undo.RecordObject(noteClip, "Set Note Index from Instrument");
                    noteClip.NoteIndex = index;
                    EditorUtility.SetDirty(noteClip);
                }
            };
            percussionButtons.Add(button);
            buttons.Add(button);
        }

        UpdateButtonStyles();
        container.Add(percussionButtons);

        // Logic to show/hide percussion buttons
        void UpdateVisibility()
        {
            var typeProp = property.FindPropertyRelative("clipType");
            percussionButtons.style.display = (AnywhenNoteClip.ClipType)typeProp.enumValueIndex == AnywhenNoteClip.ClipType.Percussion
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        UpdateVisibility();
        container.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
        {
            if (evt.changedProperty.name == "clipType")
            {
                UpdateVisibility();
            }

            if (evt.changedProperty.name == "noteIndex")
            {
                UpdateButtonStyles();
            }
        });

        return container;
    }
}

[CustomEditor(typeof(AnywhenSampleInstrument))]
public class AnywhenSampleInstrumentInspector : Editor
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

        var dropLabel = new Label("Drop AudioClips or NoteClips here")
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
        var defaultInspector = new VisualElement();
        InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);
        inspector.Add(defaultInspector);

        defaultInspector.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
        {
            if (evt.changedProperty.name == "clipType" && evt.changedProperty.propertyPath.Contains("clipDatas.Array.data["))
            {
                // We need to use serializedObject from the inspector context
                var prop = evt.changedProperty;
                var propertyPath = prop.propertyPath;
                var lastDotIndex = propertyPath.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var parentPath = propertyPath.Substring(0, lastDotIndex);
                    var parentProperty = prop.serializedObject.FindProperty(parentPath);
                    var guidProperty = parentProperty?.FindPropertyRelative("guid");
                    if (guidProperty != null)
                    {
                        var guid = guidProperty.stringValue;
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var noteClip = AssetDatabase.LoadAssetAtPath<AnywhenNoteClip>(assetPath);
                        if (noteClip != null)
                        {
                            var newType = (AnywhenNoteClip.ClipType)prop.enumValueIndex;
                            if (noteClip.Type != newType)
                            {
                                Undo.RecordObject(noteClip, "Change Clip Type from Instrument");
                                noteClip.Type = newType;
                                EditorUtility.SetDirty(noteClip);
                            }
                        }
                    }
                }
            }
        });

        // Add drop zone after the default inspector
        inspector.Add(_dropZone);
        inspector.Add(previewButton);

        return inspector;
    }

    private void OnDragEnter(DragEnterEvent evt)
    {
        if (DragAndDrop.objectReferences.Length > 0 && AreAllSupported(DragAndDrop.objectReferences))
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
        if (DragAndDrop.objectReferences.Length > 0 && AreAllSupported(DragAndDrop.objectReferences))
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

        if (DragAndDrop.objectReferences.Length > 0 && AreAllSupported(DragAndDrop.objectReferences))
        {
            AddObjectsToInstrument(DragAndDrop.objectReferences);
        }
    }

    private bool AreAllSupported(Object[] objects)
    {
        foreach (var obj in objects)
        {
            if (obj is not AudioClip && obj is not AnywhenNoteClip)
            {
                return false;
            }
        }

        return true;
    }

    private void AddObjectsToInstrument(Object[] objects)
    {
        serializedObject.Update();

        List<AnywhenNoteClip> noteClipsToAdd = new List<AnywhenNoteClip>();
        int createdCount = 0;

        // Create NoteClips for each AudioClip, and add existing NoteClips
        foreach (var obj in objects)
        {
            if (obj is AudioClip audioClip)
            {
                // Get the directory of the audio clip
                string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
                string directory = Path.GetDirectoryName(audioClipPath);

                // Create the new NoteClip in the same directory as the audio clip
                AnywhenNoteClip newNoteClip = AnywhenNoteClip.CreateNewAsset(audioClip, directory);
                noteClipsToAdd.Add(newNoteClip);
                createdCount++;
            }
            else if (obj is AnywhenNoteClip noteClip)
            {
                noteClipsToAdd.Add(noteClip);
            }
        }

        // Update the clipDatas array with the note clips
        int currentSize = _noteClipsProperty.arraySize;
        _noteClipsProperty.arraySize = currentSize + noteClipsToAdd.Count;

        for (int i = 0; i < noteClipsToAdd.Count; i++)
        {
            var element = _noteClipsProperty.GetArrayElementAtIndex(currentSize + i);
            var clipPath = AssetDatabase.GetAssetPath(noteClipsToAdd[i]);

            // Update the ClipData properties
            var nameProperty = element.FindPropertyRelative("name");
            var pathProperty = element.FindPropertyRelative("path");
            var guidProperty = element.FindPropertyRelative("guid");
            var typeProperty = element.FindPropertyRelative("clipType");

            if (nameProperty != null) nameProperty.stringValue = noteClipsToAdd[i].name;
            if (pathProperty != null) pathProperty.stringValue = clipPath;
            if (guidProperty != null) guidProperty.stringValue = AssetDatabase.AssetPathToGUID(clipPath);
            if (typeProperty != null) typeProperty.enumValueIndex = (int)noteClipsToAdd[i].Type;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_anywhenSampleInstrument);
        AssetDatabase.SaveAssets();

        // Show confirmation message
        string message = $"{noteClipsToAdd.Count} NoteClip(s) added to the instrument.";
        if (createdCount > 0)
        {
            message = $"{createdCount} NoteClip(s) created and {noteClipsToAdd.Count} total added to the instrument.";
        }

        EditorUtility.DisplayDialog("NoteClips Added", message, "OK");
    }
}