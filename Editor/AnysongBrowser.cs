using System;
using System.Collections;
using Anywhen;
using Anywhen.Composing;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
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
        private Action<bool> _onClose;
        private VisualElement _trackListView;
        private AnysongObject _currentPreviewSong;
        private AnysongPlayerControls _anysongPlayerControls;

        private VisualElement _packImageElement;
        private Label _packNameLabel, _packDescriptionLabel;

        private Button _playButton, _loadSongButton;
        private int _currentPackIndex;
        private bool _didLoad;

        public static void ShowBrowserWindow(AnywhenPlayer thisPlayer, Action<bool> onWindowClosed)
        {
            
            _anywhenPlayer = thisPlayer;
            AnysongBrowser window = (AnysongBrowser)GetWindow(typeof(AnysongBrowser));
            window.Show(true);
            window.titleContent = new GUIContent("Anysong browser");
            window.minSize = new Vector2(1000, 500);
            window.CreateGUI();
            window._onClose = onWindowClosed;
        }


        private void OnDestroy()
        {
            _anywhenPlayer?.EditorSetPreviewSong(_anywhenPlayer.AnysongObject);
            _onClose?.Invoke(_didLoad);
        }

        public void CreateGUI()
        {
            _didLoad = false;
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
            _loadSongButton.clicked += LoadSongButtonOnClicked;

            var packsHolder = ui.Q<VisualElement>("Packs");
            _trackListView = ui.Q<VisualElement>("TrackList");
            _trackListView.Clear();

            packsHolder.Clear();

            _loadSongButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            for (var i = 0; i < _packObjects.Length; i++)
            {
                var pack = _packObjects[i];
                if (!pack.IsInPackage) continue;
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

        private void LoadSongButtonOnClicked()
        {
            
            _anywhenPlayer.EditorSetSongAndPackObject(_currentPreviewSong, _currentPackIndex);
            EditorUtility.SetDirty(_anywhenPlayer);
            _anysongPlayerControls.Stop();
            _didLoad = true;
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
                loadTracksButton.clicked += () => { this.StartCoroutine(LoadPack()); };

                _trackListView.Add(loadTracksButton);
            }
        }

        IEnumerator LoadPack()
        {
            _currentPack.ClearSongs();
            EditorUtility.DisplayProgressBar("Loading songs", "Loading...", 0);
            foreach (var songName in _currentPack.songNames)
            {
                string path = AnywhenMenuUtils.GetAssetPath(songName);
                AnysongObject song = AssetDatabase.LoadAssetAtPath<AnysongObject>(path);
                _currentPack.AddSong(song);
                EditorUtility.DisplayProgressBar("Loading songs", "Loading...", (float)_currentPack.Songs.Length / _currentPack.songNames.Length);
                yield return null;
            }

            ShowSongs();
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


    }
}