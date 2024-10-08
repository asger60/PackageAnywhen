#if UNITY_EDITOR
using Anywhen;
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
        private Button _playButton, _browseButton;
        private AnysongPlayer _anysongPlayer;
        private AnysongPackObject[] _packObjects;
        private int _currentPackIndex = -1;
        private AnysongPackObject _currentPack;
        private Image _packArtImage;
        public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private Sprite _tapeSprite1, _tapeSprite2;
        private VisualElement _tapeElement;
        private VisualElement _root;

        private void OnEnable()
        {
            _anysongPlayer = target as AnysongPlayer;
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongPlayerInspector.uxml");


            //_tapeSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PackageAnywhen/Editor/Sprites/Tape1.png");
            //_tapeSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PackageAnywhen/Editor/Sprites/Tape2.png");

            VisualElement ui = uiAsset.Instantiate();
            _root.Add(ui);
            AnysongPlayerControls.HandlePlayerLogic(_root, _anysongPlayer);

            _browseButton = _root.Q<Button>("ButtonLoadTrack");
            //_playButton = _root.Q<Button>("ButtonPreview");
            _browseButton.clicked += () => { AnysongBrowser.ShowBrowserWindow(_anysongPlayer, OnBrowseWindowClosed); };

            //_playButton.clicked += Preview;

            var editButton = _root.Q<Button>("ButtonEdit");
            editButton.clicked += Edit;


            Refresh();
            _tapeElement = _root.Q<VisualElement>("TapeElement");

            var songObject = serializedObject.FindProperty("songObject");
            var songObjectField = new PropertyField(songObject);
            songObjectField.BindProperty(songObject);
            _root.Add(songObjectField);

            var triggerObject = serializedObject.FindProperty("trigger");


            var triggerObjectField = new PropertyField(triggerObject);
            triggerObjectField.BindProperty(triggerObject);
            _root.Add(triggerObjectField);


            var transitionTypeObject = serializedObject.FindProperty("triggerTransitionsType");
            var transitionObjectField = new PropertyField(transitionTypeObject);
            transitionObjectField.BindProperty(transitionTypeObject);
            _root.Add(transitionObjectField);


            return _root;
        }

        private void OnBrowseWindowClosed()
        {
            Refresh();
        }


        void Refresh()
        {
            _currentPackIndex = _anysongPlayer ? _anysongPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnysongPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];

            var labelTitle = _root.Q<Label>("LabelSongTitle");
            var labelAuthor = _root.Q<Label>("LabelSongAuthor");

            labelTitle.text = "Song: " + _anysongPlayer.AnysongObject.name;
            labelAuthor.text = "By: " + _anysongPlayer.AnysongObject.author;


            var packArtElement = _root.Q<VisualElement>("PackImage");
            packArtElement.style.backgroundImage = new StyleBackground(_currentPack.packImage);
            packArtElement.style.backgroundColor = new StyleColor(_currentPack.editorBackgroundColor);
        }

        void Preview()
        {
            var anysongPlayer = target as AnysongPlayer;
            if (anysongPlayer == null) return;

            AnywhenRuntime.TogglePreviewMode(anysongPlayer);
            AnywhenRuntime.Metronome.SetTempo(anysongPlayer.AnysongObject.tempo);

            if (AnywhenRuntime.IsPreviewing)
            {
                _playButton.style.backgroundColor = new StyleColor(AccentColor);
                AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            }
            else
            {
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                _playButton.style.backgroundColor = new StyleColor(Color.clear);
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


        private void OnTick16()
        {
            var sprite = AnywhenMetronome.Instance.Sub16 % 2 == 0 ? _tapeSprite1 : _tapeSprite2;
            _tapeElement.style.backgroundImage = new StyleBackground(sprite);
        }
    }
}
#endif