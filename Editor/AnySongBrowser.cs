using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

public class AnySongBrowser : EditorWindow
{
    private Button _previewButton, _loadButton;

    private static AnysongPlayer _anysongPlayer;
    private VisualElement _currentPackButtonHolder;
    private VisualElement _currentSongButtonHolder;
    private VisualElement _packArtHolder;
    private AnyTrackPackObject[] _packObjects;
    private int _currentPackIndex = -1;
    private AnyTrackPackObject _currentPack;
    private int _currentSongIndex = -1;
    private Image _packArtImage;
    VisualElement _songBrowserHolder, _songControlsHolder;
    char[] spinner1 = new char[] { '|', '/', '-', '\\' };

    private bool _isLoadingPack;
    private AsyncOperationHandle<IList<AnysongObject>> _loadStatus;

    public static void ShowBrowserWindow(AnysongPlayer thisPlayer)
    {
        _anysongPlayer = thisPlayer;
        AnySongBrowser window = (AnySongBrowser)GetWindow(typeof(AnySongBrowser));
        window.Show(true);
        window.titleContent = new GUIContent("Anysong browser");
        window.minSize = new Vector2(450, 200);
        window.CreateGUI();
    }

    public void CreateGUI()
    {
        _currentPackIndex = _anysongPlayer ? _anysongPlayer.currentSongPackIndex : 0;
        _packObjects = Resources.LoadAll<AnyTrackPackObject>("/");
        _currentPack = _packObjects[_currentPackIndex];
        _currentSongIndex = _anysongPlayer ? _anysongPlayer.currentSongIndex : 0;
        _songControlsHolder = new VisualElement();
        _songBrowserHolder = new VisualElement();

        rootVisualElement.Clear();


        rootVisualElement.Add(DrawSongBrowser());
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
            image = _currentPack.packImage,
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


        packBrowserButtons.Add(DrawArrowButton(-1, () => { IncrementPackSelection(-1); }));
        packBrowserButtons.Add(_currentPackButtonHolder);
        packBrowserButtons.Add(DrawArrowButton(1, () => { IncrementPackSelection(1); }));


        RefreshCurrentPack();
        RefreshCurrentSong();


        inspector.Add(packBrowserButtons);
        inspector.Add(_packArtHolder);
        //inspector.Add(songBrowserButtons);
        return inspector;
    }

    void RefreshCurrentSong()
    {
        _currentSongButtonHolder.Clear();
        if (_currentPack.Songs == null || _currentPack.Songs.Length == 0)
            return;

        var currentSongButton = new Button
        {
            style = { flexGrow = 1 }
        };


        _currentSongIndex = Mathf.Min(_currentSongIndex, _currentPack.Songs.Length - 1);
        currentSongButton.text = _currentPack.Songs[_currentSongIndex].name;


        currentSongButton.clicked += () =>
        {
            for (var i = 0; i < _currentPack.Songs.Length; i++)
            {
                var songObject = _currentPack.Songs[i];
                var songButton = new Button
                {
                    text = songObject.name
                };
                var i1 = i;
                songButton.clicked += () =>
                {
                    _currentSongIndex = i1;
                    RefreshCurrentSong();
                    _anysongPlayer.SetSongObject(_currentPack.Songs[_currentSongIndex], i1);
                };
                _currentSongButtonHolder.Add(songButton);
            }
        };

        _currentSongButtonHolder.Add(currentSongButton);
    }

    void RefreshCurrentPack()
    {
        _songControlsHolder.Clear();
        _currentPackButtonHolder.Clear();
        _songBrowserHolder.Clear();
        var currentPackButton = new Button
        {
            text = _packObjects[_currentPackIndex].name,
            style =
            {
                flexGrow = 1,
            }
        };
        currentPackButton.clicked += () =>
        {
            for (var i = 0; i < _packObjects.Length; i++)
            {
                var trackPackObject = _packObjects[i];
                var packButton = new Button
                {
                    text = trackPackObject.name
                };
                var i1 = i;
                packButton.clicked += () =>
                {
                    _currentPackIndex = i1;
                    _currentPack = _packObjects[_currentPackIndex];
                    _currentSongIndex = 0;
                    RefreshCurrentPack();
                    RefreshCurrentSong();
                };

                _currentPackButtonHolder.Add(packButton);
            }

            _currentPackButtonHolder.Remove(currentPackButton);
        };
        _packArtImage.image = _currentPack.packImage;
        _currentPackButtonHolder.Add(currentPackButton);

        _songBrowserHolder.Add(DrawSongSelector());
        _songControlsHolder.Add(DrawSongControls());
    }

    void IncrementPackSelection(int direction)
    {
        _currentPackIndex += direction;
        _currentPackIndex = (int)Mathf.Repeat(_currentPackIndex, _packObjects.Length);

        _currentPack = _packObjects[_currentPackIndex];
        _currentSongIndex = 0;

        RefreshCurrentSong();
        RefreshCurrentPack();
        EditorUtility.SetDirty(_anysongPlayer);
    }

    void IncrementSongSelection(int direction)
    {
        if (_currentPack.Songs == null || _currentPack.Songs.Length == 0)
        {
            return;
        }

        _currentSongIndex += direction;
        _currentSongIndex = (int)Mathf.Repeat(_currentSongIndex, _currentPack.Songs.Length);
        RefreshCurrentSong();
    }

    void LoadSongToPlayer()
    {
        _anysongPlayer.SetSongPackIndex(_currentPackIndex);
        _anysongPlayer.SetSongObject(_currentPack.Songs[_currentSongIndex], _currentSongIndex);
        EditorUtility.SetDirty(_anysongPlayer);
        _anysongPlayer.SetPreviewSong(null);
        AnywhenRuntime.SetPreviewMode(false, _anysongPlayer);
        this.Close();
    }


    void LoadPack()
    {
        Debug.Log("loading pack");
        _loadStatus = AnySongPackInspector.LoadSongs(_currentPack);
        _isLoadingPack = true;
    }


    void Update()
    {
        if (!_isLoadingPack) return;
        Debug.Log("loading.." + _loadStatus.PercentComplete);
        EditorUtility.DisplayProgressBar("Loading pack", "Loading songs...", _loadStatus.PercentComplete);
        if (_loadStatus.IsDone)
        {
            if (_loadStatus.Status == AsyncOperationStatus.Succeeded)
            {
                _currentPack.SetSongs(_loadStatus.Result.ToArray());
            }

            _currentSongIndex = 0;
            RefreshCurrentPack();
            RefreshCurrentSong();
            EditorUtility.ClearProgressBar();
            _isLoadingPack = false;
            Debug.Log("load done");
            Addressables.Release(_loadStatus);
        }
    }

    void PreviewSong()
    {
        _anysongPlayer.SetPreviewSong(_currentPack.Songs[_currentSongIndex]);
        AnywhenRuntime.Metronome.SetTempo(_currentPack.Songs[_currentSongIndex].tempo);

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
        _previewButton.text = spinner1[(int)Mathf.Repeat(AnywhenRuntime.Metronome.Sub16, 4)] + " Previewing";
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

    VisualElement DrawSongControls()
    {
        var visualElement = new VisualElement();
        if (_currentPack.Songs != null && _currentPack.Songs.Length > 0)
        {
            _previewButton = new Button
            {
                text = "Preview"
            };
            _previewButton.clicked += PreviewSong;

            _loadButton = new Button
            {
                text = "Load"
            };
            _loadButton.clicked += LoadSongToPlayer;

            visualElement.Add(_previewButton);
            visualElement.Add(_loadButton);
        }
        else
        {
            var loadPackButton = new Button
            {
                text = "Load pack"
            };
            loadPackButton.clicked += LoadPack;
            visualElement.Add(loadPackButton);
        }

        return visualElement;
    }

    VisualElement DrawSongSelector()
    {
        if (_currentPack.Songs == null || _currentPack.Songs.Length == 0)
        {
            return new VisualElement();
        }

        VisualElement songBrowserButtons = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };

        songBrowserButtons.Add(DrawArrowButton(-1, () => { IncrementSongSelection(-1); }));
        songBrowserButtons.Add(_currentSongButtonHolder);
        songBrowserButtons.Add(DrawArrowButton(1, () => { IncrementSongSelection(1); }));

        return songBrowserButtons;
    }
}