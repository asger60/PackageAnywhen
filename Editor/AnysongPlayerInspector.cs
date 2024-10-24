#if UNITY_EDITOR
using Anywhen;
using Anywhen.Composing;
using Editor.AnySong;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(AnywhenPlayer))]
    public class AnysongPlayerInspector : UnityEditor.Editor
    {
        private Button _playButton, _browseButton;
        private AnywhenPlayer _anywhenPlayer;
        private AnysongPackObject[] _packObjects;
        private AnysongPackObject _currentPack;
        private Image _packArtImage;
        public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private VisualElement _root;
        private AnysongPlayerControls _anysongPlayerControls;
        private int _currentPackIndex;

        private void OnEnable()
        {
            _anywhenPlayer = target as AnywhenPlayer;
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
            string path = AnywhenMenuUtils.GetAssetPath("Editor/uxml/AnysongPlayerInspector.uxml");
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);

            VisualElement ui = uiAsset.Instantiate();
            _root.Add(ui);
            _anysongPlayerControls = new AnysongPlayerControls();
            _anysongPlayerControls.HandlePlayerLogic(_root, _anywhenPlayer);

            _browseButton = _root.Q<Button>("ButtonLoadTrack");
            _browseButton.clicked += () => { AnysongBrowser.ShowBrowserWindow(_anywhenPlayer, OnBrowseWindowClosed); };


            var editButton = _root.Q<Button>("ButtonEdit");
            editButton.clicked += Edit;


            Refresh();

            var songObject = serializedObject.FindProperty("songObject");
            var songObjectField = new PropertyField(songObject);
            songObjectField.BindProperty(songObject);
            _root.Add(songObjectField);

            var triggerObject = serializedObject.FindProperty("trigger");


            var noTriggerElement = _root.Q<VisualElement>("NoTriggerElement");
            var createTriggerButton = _root.Q<Button>("CreateTriggerButton");
            var locateTriggerButton = _root.Q<Button>("LocateTriggerButton");
            createTriggerButton.clicked += CreateTriggerButtonOnclicked;
            locateTriggerButton.clicked += LocateTriggerButtonOnclicked;


            noTriggerElement.style.display = triggerObject.objectReferenceValue == null
                ? new StyleEnum<DisplayStyle>(StyleKeyword.Auto)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            var triggerObjectField = new PropertyField(triggerObject);
            triggerObjectField.BindProperty(triggerObject);
            _root.Add(triggerObjectField);


            var transitionTypeObject = serializedObject.FindProperty("triggerTransitionsType");
            var transitionObjectField = new PropertyField(transitionTypeObject);
            transitionObjectField.BindProperty(transitionTypeObject);
            _root.Add(transitionObjectField);


            return _root;
        }

        private void LocateTriggerButtonOnclicked()
        {
            _anywhenPlayer.EditorLocateTrigger();
        }

        private void CreateTriggerButtonOnclicked()
        {
            _anywhenPlayer.EditorCreateTrigger();
        }

        private void OnBrowseWindowClosed()
        {
            Refresh();
        }


        void Refresh()
        {
            if (_anywhenPlayer.AnysongObject == null) return;
            _currentPackIndex = _anywhenPlayer ? _anywhenPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnysongPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];

            var labelTitle = _root.Q<Label>("LabelSongTitle");
            var labelAuthor = _root.Q<Label>("LabelSongAuthor");

            labelTitle.text = "Song: " + _anywhenPlayer.AnysongObject.name;
            labelAuthor.text = "By: " + _anywhenPlayer.AnysongObject.author;


            var packArtElement = _root.Q<VisualElement>("PackImage");
            packArtElement.style.backgroundImage = new StyleBackground(_currentPack.packImage);
            packArtElement.style.backgroundColor = new StyleColor(_currentPack.editorBackgroundColor);
        }


        void Edit()
        {
            if (_anywhenPlayer.AnysongObject == null) return;
            var anysongPlayer = target as AnywhenPlayer;
            AnysongEditorWindow.LoadSong(anysongPlayer?.AnysongObject, anysongPlayer);
            AnysongEditorWindow.ShowModuleWindow();
        }
    }
}
#endif