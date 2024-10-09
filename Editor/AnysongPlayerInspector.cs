#if UNITY_EDITOR
using Anywhen;
using Editor.AnySong;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(AnysongPlayer))]
    public class AnysongPlayerInspector : UnityEditor.Editor
    {
        private Button _playButton, _browseButton;
        private AnysongPlayer _anysongPlayer;
        private AnysongPackObject[] _packObjects;
        private AnysongPackObject _currentPack;
        private Image _packArtImage;
        public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private VisualElement _root;
        private AnysongPlayerControls _anysongPlayerControls;
        private int _currentPackIndex;

        private void OnEnable()
        {
            _anysongPlayer = target as AnysongPlayer;
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongPlayerInspector.uxml");

            VisualElement ui = uiAsset.Instantiate();
            _root.Add(ui);
            _anysongPlayerControls = new AnysongPlayerControls();
            _anysongPlayerControls.HandlePlayerLogic(_root, _anysongPlayer);

            _browseButton = _root.Q<Button>("ButtonLoadTrack");
            _browseButton.clicked += () => { AnysongBrowser.ShowBrowserWindow(_anysongPlayer, OnBrowseWindowClosed); };


            var editButton = _root.Q<Button>("ButtonEdit");
            editButton.clicked += Edit;


            Refresh();

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



        void Edit()
        {
            var anysongPlayer = target as AnysongPlayer;
            AnysongEditorWindow.LoadSong(anysongPlayer?.AnysongObject, anysongPlayer);

            AnysongEditorWindow.ShowModuleWindow();
        }





    }
}
#endif