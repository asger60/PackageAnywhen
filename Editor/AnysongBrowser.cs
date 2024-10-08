using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

public class AnysongBrowser : EditorWindow
{
    private Button _previewButton, _loadButton;
    private static AnysongPlayer _anysongPlayer;
    private VisualElement _currentPackButtonHolder;
    private VisualElement _currentSongButtonHolder;
    private VisualElement _packArtHolder;
    private AnysongPackObject[] _packObjects;
    private int _currentPackIndex = -1;
    private AnysongPackObject _currentPack;
    private Image _packArtImage;
    VisualElement _songBrowserHolder, _songControlsHolder;

    private bool _isLoadingPack;
    private AsyncOperationHandle<IList<AnysongObject>> _loadStatus;
    private Action OnClose;
    private VisualElement _root;
    private VisualElement _trackListView;
    private AnysongObject _currentPreviewSong;
    private AnysongPlayerControls _anysongPlayerControls;
    public static void ShowBrowserWindow(AnysongPlayer thisPlayer, Action OnWindowClosed)
    {
        _anysongPlayer = thisPlayer;
        AnysongBrowser window = (AnysongBrowser)GetWindow(typeof(AnysongBrowser));
        window.Show(true);
        window.titleContent = new GUIContent("Anysong browser");
        window.minSize = new Vector2(450, 200);
        window.CreateGUI();
        window.OnClose = OnWindowClosed;
    }


    private void OnDestroy()
    {
        OnClose?.Invoke();
    }

    public void CreateGUI()
    {
        VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongBrowser.uxml");
        VisualElement ui = uiAsset.Instantiate();

        _currentPackIndex = _anysongPlayer ? _anysongPlayer.currentSongPackIndex : 0;
        _packObjects = Resources.LoadAll<AnysongPackObject>("/");
        _currentPack = _packObjects[_currentPackIndex];
        _songControlsHolder = new VisualElement();
        _songBrowserHolder = new VisualElement();

        rootVisualElement.Clear();
        rootVisualElement.Add(ui);

        _anysongPlayerControls= new AnysongPlayerControls();
        _anysongPlayerControls.HandlePlayerLogic(rootVisualElement, _anysongPlayer);
        
        
        var packsHolder = ui.Q<VisualElement>("Packs");
        _trackListView = ui.Q<VisualElement>("TrackList");
        
        packsHolder.Clear();
        foreach (var pack in _packObjects)
        {
            Button packElement = new Button()
            {
                style =
                {
                    width = new StyleLength(200),
                    height = new StyleLength(200),
                    backgroundImage = new StyleBackground(pack.packImage)
                }
            };
            packElement.clicked += () =>
            {
                Debug.Log("Clicked " + pack.name);
                LoadPack(pack);
            };

            packsHolder.Add(packElement);
        }


        rootVisualElement.Add(_songBrowserHolder);
        rootVisualElement.Add(_songControlsHolder);
    }

    
    

    void LoadPack(AnysongPackObject packObject)
    {
        Debug.Log("loading pack " + packObject.AssetLabelReference.labelString);
        _currentPack = packObject;
        _currentPack.ClearSongs();
        _noIncrementFrames = 0;
        _lastFramCount = 0;
        
        _loadStatus = new AsyncOperationHandle<IList<AnysongObject>>();
        _loadStatus = Addressables.LoadAssetsAsync<AnysongObject>(packObject.AssetLabelReference,
            song =>
            {
                Debug.Log("loaded: " + song.name);
                _currentPack.AddSong(song);
            });


        _isLoadingPack = true;
    }

    void LoadCompletedCallback(AsyncOperationHandle<IList<AnysongObject>> songs)
    {
        _isLoadingPack = false;
        _trackListView.Clear();

        
       foreach (var song in _currentPack.Songs)
       {
           var newTrackButton = new Button
           {
               text = song.name
           };
           
           _trackListView.Add(newTrackButton);
           newTrackButton.clicked += () =>
           {
               SetPreviewSong(song);
           };

       }
        Debug.Log("load completed");

    }

    void SetPreviewSong(AnysongObject song)
    {
        Debug.Log("current preview song: " + song.name);
        _currentPreviewSong = song;
        _anysongPlayerControls.SetSongObject(_currentPreviewSong);
    }


    private int _noIncrementFrames;
    private int _lastFramCount;
    private Button _playButton;
    private int _currentSongIndex;

    void Update()
    {
        if (!_isLoadingPack) return;
        Debug.Log("loading.. " + _currentPack.Songs.Length);
        if (_currentPack.Songs.Length == _lastFramCount)
            _noIncrementFrames++;
        
        _lastFramCount = _currentPack.Songs.Length;

        if (_noIncrementFrames > 10)
            LoadCompletedCallback(_loadStatus);

        if (_loadStatus.IsDone)
            LoadCompletedCallback(_loadStatus);
        else
            _loadStatus.Completed += LoadCompletedCallback;

    }




    
}