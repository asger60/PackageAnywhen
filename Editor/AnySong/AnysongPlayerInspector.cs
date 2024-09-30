#if UNITY_EDITOR
using System;
using System.Linq;
using Anywhen;
using Anywhen.Composing;
using Editor.AnySong;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Anysong
{
    [CustomEditor(typeof(AnysongPlayer))]
    public class AnysongPlayerInspector : UnityEditor.Editor
    {
        private Button _playButton;
        private AnysongPlayer _anysongPlayer;
        private VisualElement _currentPackButtonHolder;
        private VisualElement _currentSongButtonHolder;
        private VisualElement _packArtHolder;
        private AnyTrackPackObject[] _packObjects;
        private int _currentPackIndex = -1;
        private AnyTrackPackObject _currentPack;
        private int _currentSongIndex = -1;
        private Image _packArtImage;

        private void OnEnable()
        {
            _anysongPlayer = target as AnysongPlayer;
            _currentPackIndex = _anysongPlayer ? _anysongPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnyTrackPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];
            _currentSongIndex = _anysongPlayer ? _anysongPlayer.currentSongIndex : 0;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(DrawSongBrowser());


            VisualElement utilityButtonsElement = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };


            _playButton = new Button
            {
                text = "Preview"
            };
            _playButton.clicked += Preview;

            var editButton = new Button
            {
                text = "Open in editor",
            };
            editButton.clicked += Edit;


            utilityButtonsElement.Add(_playButton);
            utilityButtonsElement.Add(editButton);
            inspector.Add(utilityButtonsElement);


            var songObject = serializedObject.FindProperty("songObject");
            var songObjectField = new PropertyField(songObject);
            songObjectField.BindProperty(songObject);
            inspector.Add(songObjectField);

            var triggerObject = serializedObject.FindProperty("trigger");


            var triggerObjectField = new PropertyField(triggerObject);
            triggerObjectField.BindProperty(triggerObject);
            inspector.Add(triggerObjectField);


            var transitionTypeObject = serializedObject.FindProperty("triggerTransitionsType");
            var transitionObjectField = new PropertyField(transitionTypeObject);
            transitionObjectField.BindProperty(transitionTypeObject);
            inspector.Add(transitionObjectField);


            return inspector;
        }


        void Preview()
        {
            var anysongPlayer = target as AnysongPlayer;
            if (anysongPlayer == null) return;
            //AnysongEditorWindow.SetPlayer(anysongPlayer);
            anysongPlayer?.ToggleEditorPreview();
            AnywhenRuntime.Metronome.SetTempo(anysongPlayer.AnysongObject.tempo);
            
            

            if (anysongPlayer.IsPreviewing)
                AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            else
            {
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                _playButton.text = "Preview";
            }
        }

        void Edit()
        {
            var anysongPlayer = target as AnysongPlayer;
            AnysongEditorWindow.LoadSong(anysongPlayer?.AnysongObject, anysongPlayer);

            AnysongEditorWindow.ShowModuleWindow();
        }

        private void OnDestroy()
        {
            if (AnywhenRuntime.Metronome)
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }

        //private string _spinner = "← ↖ ↑ ↗ → ↘ ↓ ↙";

        // Define the spinner characters
        char[] spinner1 = new char[] { '|', '/', '-', '\\' };

        char[] spinner2 = new char[]
        {
            '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'
        };

        private void OnTick16()
        {
            _playButton.text = spinner1[(int)Mathf.Repeat(AnywhenRuntime.Metronome.Sub16, 4)] + " Previewing";
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


            VisualElement songBrowserButtons = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

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


            songBrowserButtons.Add(DrawArrowButton(-1, () => { IncrementSongSelection(-1); }));
            songBrowserButtons.Add(_currentSongButtonHolder);
            songBrowserButtons.Add(DrawArrowButton(1, () => { IncrementSongSelection(1); }));


            RefreshCurrentPack();
            RefreshCurrentSong();


            inspector.Add(packBrowserButtons);
            inspector.Add(_packArtHolder);
            inspector.Add(songBrowserButtons);
            return inspector;
        }

        void RefreshCurrentSong()
        {
            _currentSongButtonHolder.Clear();
            Debug.Log("refresh songs");
            
            var currentSongButton = new Button
            {
                style = { flexGrow = 1 }
            };
            
            if (_currentPack.Songs == null || _currentPack.Songs.Length == 0)
            {
                currentSongButton.text = "load tracks";
                currentSongButton.clicked += () =>
                {
                    var loadSongs = AnySongPackInspector.LoadSongs(_currentPack);
                    loadSongs.Completed += handle =>
                    {
                        _currentPack.SetSongs(handle.Result.ToArray());
                        RefreshCurrentSong();
                    };
                };
                _currentSongButtonHolder.Add(currentSongButton);

                return;
                
            }

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

               // _currentSongButtonHolder.Add(currentSongButton);
            };

            _currentSongButtonHolder.Add(currentSongButton);
        }

        void RefreshCurrentPack()
        {
            _currentPackButtonHolder.Clear();
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
            _anysongPlayer.SetSongPackIndex(_currentPackIndex);
        }

        void IncrementPackSelection(int direction)
        {
            _currentPackIndex += direction;
            _currentPackIndex = (int)Mathf.Repeat(_currentPackIndex, _packObjects.Length);

            _currentPack = _packObjects[_currentPackIndex];
            _currentSongIndex = 0;
            
            RefreshCurrentSong();
            RefreshCurrentPack();
            EditorUtility.SetDirty(target);

        }

        void IncrementSongSelection(int direction)
        {
            _currentSongIndex += direction;
            _currentSongIndex = (int)Mathf.Repeat(_currentSongIndex, _currentPack.Songs.Length);
            _anysongPlayer.SetSongObject(_currentPack.Songs[_currentSongIndex], _currentSongIndex);
            RefreshCurrentSong();
            EditorUtility.SetDirty(target);
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
}
#endif