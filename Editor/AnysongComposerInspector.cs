#if UNITY_EDITOR
using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


[CustomEditor(typeof(AnywhenComposerPlayer))]
public class AnysongComposerInspector : Editor
{
    private Button _playButton, _browseButton;
    private AnywhenComposerPlayer _anywhenPlayer;
    private AnysongPackObject[] _packObjects;
    private AnysongPackObject _currentPack;
    private Image _packArtImage;
    public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
    private VisualElement _root;
    private AnysongPlayerControls _anysongPlayerControls;
    private int _initialTempo;
    private AnySlider _intensityAnySlider, _tempoAnySlider;

    private void OnEnable()
    {
        _anywhenPlayer = target as AnywhenComposerPlayer;
        var anywhen = FindFirstObjectByType<AnywhenRuntime>();
        if (!anywhen)
        {
            Debug.LogWarning("no anywhen");
            AnywhenMenuUtils.AddAnywhen();
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        _root = new VisualElement();
     
        // Create a container for the Anysong object selection
        var songObjectContainer = new VisualElement()
        {
            style =
            {
                marginTop = 10,
                marginBottom = 10
            }
        };
        
        // Create a property field for the song object
        var songObjectProperty = serializedObject.FindProperty("currentSong");
        var songObjectField = new PropertyField(songObjectProperty);
        songObjectField.BindProperty(songObjectProperty);
        songObjectContainer.Add(songObjectField);
        
        // Add the container to the root
        _root.Add(songObjectContainer);
        
        // Create edit button
        
        var editButton = new Button(Edit)
        {
            text = "Open Anysong Editor",
            style =
            {
                height = 30,
                backgroundColor = AccentColor,
                marginTop = 10,
                marginBottom = 10
            }
        };
        
        _root.Add(editButton);
        

        
        return _root;
    }

    
    void Edit()
    {
        if (_anywhenPlayer.CurrentSong == null) return;
        AnysongEditorWindow.LoadSong(_anywhenPlayer.CurrentSong, _anywhenPlayer);
        AnysongEditorWindow.ShowModuleWindow();
    }

    void LoadSounds()
    {
        _anywhenPlayer.LoadInstruments();
    }
}

#endif