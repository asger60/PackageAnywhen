using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace Editor
{
    public class AnysongBrowser : EditorWindow
    {
        private Button _previewButton;
        private static AnywhenPlayer _anywhenPlayer;
        private AnysongPackObject[] _packObjects;
        private AnysongPackObject _currentPack;

        private bool _isLoadingPack;
        private AsyncOperationHandle<IList<AnysongObject>> _loadStatus;
        private Action _onClose;
        private VisualElement _trackListView;
        private AnysongObject _currentPreviewSong;
        private AnysongPlayerControls _anysongPlayerControls;

        private VisualElement _packImageElement;
        private Label _packNameLabel, _packDescriptionLabel;
        private int _noIncrementFrames;
        private int _lastFrameCount;
        private Button _playButton, _loadSongButton;
        private int _currentPackIndex;

        public static void ShowBrowserWindow(AnywhenPlayer thisPlayer, Action OnWindowClosed)
        {
            _anywhenPlayer = thisPlayer;
            AnysongBrowser window = (AnysongBrowser)GetWindow(typeof(AnysongBrowser));
            window.Show(true);
            window.titleContent = new GUIContent("Anysong browser");
            window.minSize = new Vector2(450, 200);
            window.CreateGUI();
            window._onClose = OnWindowClosed;
        }


        private void OnDestroy()
        {
            _anywhenPlayer.EditorSetPreviewSong(null);
            _onClose?.Invoke();
        }

        public void CreateGUI()
        {
            string path = AnywhenMenuUtils.GetAssetPath("Editor/uxml/AnysongBrowser.uxml");
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            VisualElement ui = uiAsset.Instantiate();

            _currentPackIndex = _anywhenPlayer ? _anywhenPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnysongPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];


            rootVisualElement.Clear();
            rootVisualElement.Add(ui);

            _anysongPlayerControls = new AnysongPlayerControls();
            _anysongPlayerControls.HandlePlayerLogic(rootVisualElement, _anywhenPlayer);


            _packImageElement = ui.Q<VisualElement>("PackImage666");
            _packNameLabel = ui.Q<Label>("PackName");
            _packDescriptionLabel = ui.Q<Label>("PackDescription");
            _loadSongButton = ui.Q<Button>("LoadSongButton");
            _loadSongButton.clicked += LoadSongButtonOnclicked;

            var packsHolder = ui.Q<VisualElement>("Packs");
            _trackListView = ui.Q<VisualElement>("TrackList");
            _trackListView.Clear();

            packsHolder.Clear();

            _loadSongButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            for (var i = 0; i < _packObjects.Length; i++)
            {
                
                var pack = _packObjects[i];
                if(!pack.IsInPackage) continue;
                Button packElement = new Button()
                {
                    style =
                    {
                        width = new StyleLength(200),
                        height = new StyleLength(200),
                        backgroundImage = new StyleBackground(pack.packImage)
                    }
                };
                var i1 = i;
                packElement.clicked += () => { ShowPack(pack, i1); };

                packsHolder.Add(packElement);
            }
        }

        private void LoadSongButtonOnclicked()
        {
            _anywhenPlayer.EditorSetSongAndPackObject(_currentPreviewSong, _currentPackIndex);
            EditorUtility.SetDirty(_anywhenPlayer);
            _anysongPlayerControls.Stop();
            Close();
        }


        void ShowPack(AnysongPackObject packObject, int index)
        {
            _currentPackIndex = index;
            _loadSongButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            _currentPack = packObject;

            _packImageElement.style.width = 200;
            _packImageElement.style.height = 200;
            _packImageElement.style.backgroundImage = new StyleBackground(_currentPack.packImage);
            _packImageElement.style.backgroundColor = new StyleColor(Color.red);


            _packNameLabel.text = _currentPack.name;
            _packDescriptionLabel.text = _currentPack.description;
            _trackListView.Clear();

            if (_currentPack.Songs != null && _currentPack.Songs.Length > 0)
            {
                ShowSongs();
            }
            else
            {
                Button loadTracksButton = new Button
                {
                    text = "Load tracks"
                };
                loadTracksButton.clicked += LoadPack;
                _trackListView.Add(loadTracksButton);
            }
        }

        void LoadPack()
        {
            _currentPack.ClearSongs();
            _noIncrementFrames = 0;
            _lastFrameCount = 0;
            EditorUtility.DisplayProgressBar("Loading songs", "Loading...", 0);
            foreach (var songName in _currentPack.songNames)
            {
                string path = AnywhenMenuUtils.GetAssetPath(songName);
                AnysongObject song = AssetDatabase.LoadAssetAtPath<AnysongObject>(path);
                _currentPack.AddSong(song);
            }
            
            ShowSongs();
            //_loadStatus = new AsyncOperationHandle<IList<AnysongObject>>();
            //_loadStatus = Addressables.LoadAssetsAsync<AnysongObject>(_currentPack.AssetLabelReference,
            //    song =>
            //    {
            //        _currentPack.AddSong(song);
            //    });
//
//
            //_isLoadingPack = true;
        }

        void LoadCompletedCallback(AsyncOperationHandle<IList<AnysongObject>> songs)
        {
            _isLoadingPack = false;
            ShowSongs();
            Debug.Log("load completed");
            
        }

        void ShowSongs()
        {
            EditorUtility.ClearProgressBar();
            _loadSongButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            _trackListView.Clear();
            foreach (var song in _currentPack.Songs)
            {
                var newTrackButton = new Button
                {
                    text = song.name
                };
                newTrackButton.AddToClassList("tracklist-button");

                _trackListView.Add(newTrackButton);
                newTrackButton.clicked += () =>
                {
                    SetPreviewSong(song);
                    if (_anysongPlayerControls.IsPlaying)
                    {
                        _anysongPlayerControls.Play();
                    }
                };
            }
        }

        void SetPreviewSong(AnysongObject song)
        {
            _currentPreviewSong = song;
            _anysongPlayerControls.SetSongObject(_currentPreviewSong);
            _loadSongButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }


        void OnInspectorUpdate()
        {
            if (!_isLoadingPack) return;
            if (_currentPack.Songs.Length == _lastFrameCount)
                _noIncrementFrames++;

            _lastFrameCount = _currentPack.Songs.Length;

            EditorUtility.DisplayProgressBar("Loading songs", "Loading...", _noIncrementFrames / 4f);

            if (_noIncrementFrames > 3)
                LoadCompletedCallback(_loadStatus);

            if (_loadStatus.IsDone)
                LoadCompletedCallback(_loadStatus);
        }
    }
}