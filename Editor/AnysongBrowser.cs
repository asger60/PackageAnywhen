using System;
using System.Collections.Generic;
using System.Linq;
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
    char[] spinner1 = new char[] { '|', '/', '-', '\\' };

    private bool _isLoadingPack;
    private AsyncOperationHandle<IList<AnysongObject>> _loadStatus;
    private Action OnClose;
    private VisualElement _root;
    private VisualElement _trackListView;
    private AnysongObject _currentPreviewSong;
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
        
        AnysongPlayerControls.HandlePlayerLogic(rootVisualElement, _anysongPlayer);
        
        var packsHolder = ui.Q<VisualElement>("Packs");
        _trackListView = ui.Q<VisualElement>("TrackList");
        //_playButton = ui.Q<Button>("ButtonPreview");
        //_playButton.clicked += PreviewSong;

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


        //rootVisualElement.Add(DrawSongBrowser());
        rootVisualElement.Add(_songBrowserHolder);

        rootVisualElement.Add(_songControlsHolder);
    }


    VisualElement DrawSongBrowser()
    {
        VisualElement inspector = new VisualElement();

        _packArtHolder = new VisualElement
        {
            style =
            {
                height = 240
            }
        };
        _packArtImage = new Image
        {
            //image = _currentPack.packImage,
            scaleMode = ScaleMode.ScaleToFit,
            style =
            {
                width = new StyleLength(new Length(100, LengthUnit.Percent)),
            }
        };
        _packArtHolder.Add(_packArtImage);


        _currentPackButtonHolder = new VisualElement();
        _currentPackButtonHolder.style.flexGrow = 1;
        _currentSongButtonHolder = new VisualElement();
        _currentSongButtonHolder.style.flexGrow = 1;


        VisualElement packBrowserButtons = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };




        inspector.Add(packBrowserButtons);
        inspector.Add(_packArtHolder);
        //inspector.Add(songBrowserButtons);
        return inspector;
    }




    void LoadSongToPlayer()
    {
        _anysongPlayer.SetSongPackIndex(_currentPackIndex);
        _anysongPlayer.SetSongObject(_currentPreviewSong, _currentSongIndex);
        EditorUtility.SetDirty(_anysongPlayer);
        _anysongPlayer.SetPreviewSong(null);
        AnywhenRuntime.SetPreviewMode(false, _anysongPlayer);
        _anysongPlayer.RefreshUI();
        OnClose?.Invoke();
        Close();
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
        AnysongPlayerControls.SetSongObject(_currentPreviewSong);
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


        //if (_loadStatus.IsDone)
        //{
        //    _isLoadingPack = false;
        //    Addressables.Release(_loadStatus);
        //}

        // Addressables.Release(_loadStatus);
        //EditorUtility.DisplayProgressBar("Loading pack", "Loading songs...", _loadStatus.PercentComplete);
        //if (_loadStatus.IsDone)
        //{
        //    if (_loadStatus.Status == AsyncOperationStatus.Succeeded)
        //    {
        //        _currentPack.SetSongs(_loadStatus.Result.ToArray());
        //    }
//
        //    _currentSongIndex = 0;
        //    RefreshCurrentPack();
        //    RefreshCurrentSong();
        //    //EditorUtility.ClearProgressBar();
        //    _isLoadingPack = false;
        //    Debug.Log("load done");
        //    Addressables.Release(_loadStatus);
        //}
    }

    void PreviewSong()
    {
        if (_currentPreviewSong == null)
        {
            Debug.Log("no song");
            return;
        }
        _anysongPlayer.SetPreviewSong(_currentPreviewSong);
        AnywhenRuntime.Metronome.SetTempo(_currentPreviewSong.tempo);

        AnywhenRuntime.TogglePreviewMode(_anysongPlayer);

        if (AnywhenRuntime.IsPreviewing)
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
        else
        {
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            _previewButton.text = "Preview";
        }
    }


    private void OnTick16()
    {
        //_previewButton.text = spinner1[(int)Mathf.Repeat(AnywhenRuntime.Metronome.Sub16, 4)] + " Previewing";
    }

    VisualElement DrawArrowButton(int direction, Action onClick)
    {
        var nextButton = new Button
        {
            text = direction == 1 ? ">" : "<",
            style = { width = 30, }
        };
        nextButton.clicked += onClick.Invoke;
        return nextButton;
    }


    
}