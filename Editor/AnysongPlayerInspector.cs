#if UNITY_EDITOR
using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


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
        private int _initialTempo;
        private AnySlider _intensityAnySlider, _tempoAnySlider;
        
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
            _browseButton.clicked += () =>
            {
                _anysongPlayerControls.Stop();

                _initialTempo = _anywhenPlayer.GetTempo();
                AnysongBrowser.ShowBrowserWindow(_anywhenPlayer, OnBrowseWindowClosed);
            };

            
            _intensityAnySlider = _root.Q<AnySlider>("IntensitySlider");
            _tempoAnySlider = _root.Q<AnySlider>("TempoSlider");
            _tempoAnySlider.SetValueWithoutNotify(_anywhenPlayer.GetTempo());
            _intensityAnySlider.SetValueWithoutNotify(_anywhenPlayer.GetIntensity() * 100f);

            _intensityAnySlider.RegisterValueChangedCallback(evt =>
            {
                _anywhenPlayer.SetIntensity(evt.newValue / 100f);
                EditorUtility.SetDirty(_anywhenPlayer);
            });
            _tempoAnySlider.RegisterValueChangedCallback(evt =>
            {
                _anywhenPlayer.EditorSetTempo((int)evt.newValue);
                EditorUtility.SetDirty(_anywhenPlayer);
            });

            var randomizeButton = _root.Q<Button>("ButtonRandomizeInstruments");
            randomizeButton.clicked += () =>
            {
                bool wasPlaying = AnywhenRuntime.IsPreviewing;
                _anysongPlayerControls.Stop();
                _anywhenPlayer.EditorRandomizeSounds();
                if (wasPlaying)
                {
                    _anysongPlayerControls.Play();
                }
            };

            _root.Query<Button>("RootNoteButton").ForEach(button =>
            {
                button.clicked += () =>
                {
                    _anywhenPlayer.EditorSetRootNote(Int32.Parse(button.text));
                    RefreshActiveRootNoteButton();
                };
            });
            RefreshActiveRootNoteButton();
            var restoreButton = _root.Q<Button>("ButtonRestoreInstruments");
            restoreButton.clicked += () => { _anywhenPlayer.EditorRestoreSounds(); };

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


            var globalTempoToggle = _root.Q<Toggle>("FollowTempoToggle");
            var globalIntensityToggle = _root.Q<Toggle>("FollowIntensityToggle");
            globalIntensityToggle.SetValueWithoutNotify(_anywhenPlayer.GetUseGlobalIntensity());
            globalTempoToggle.SetValueWithoutNotify(_anywhenPlayer.GetUseGlobalTempo());
            _intensityAnySlider.SetIsEnabled(!_anywhenPlayer.GetUseGlobalIntensity());
            _tempoAnySlider.SetIsEnabled(!_anywhenPlayer.GetUseGlobalIntensity());
            
            

            globalTempoToggle.RegisterValueChangedCallback(evt =>
            {
                _tempoAnySlider.SetIsEnabled( !evt.newValue);
                _anywhenPlayer.EditorSetGlobelTempo(evt.newValue);
            });
            globalIntensityToggle.RegisterValueChangedCallback(evt =>
            {
                _intensityAnySlider.SetIsEnabled(!evt.newValue);
                _anywhenPlayer.EditorSetFollowGlobalIntensity(evt.newValue);
            });


            return _root;
        }

        void RefreshActiveRootNoteButton()
        {
            _root.Query<Button>("RootNoteButton").ForEach(button =>
            {
                if (Int32.Parse(button.text) == _anywhenPlayer.GetRootNote())
                {
                    button.style.backgroundColor = new StyleColor(Color.grey);
                }
                else
                {
                    button.style.backgroundColor = new StyleColor(Color.clear);
                }
            });
        }

        private void LocateTriggerButtonOnclicked()
        {
            _anywhenPlayer.EditorLocateTrigger();
        }

        private void CreateTriggerButtonOnclicked()
        {
            _anywhenPlayer.EditorCreateTrigger();
        }

        private void OnBrowseWindowClosed(bool didLoad)
        {
            if (!didLoad)
            {
                _anywhenPlayer.EditorSetTempo(_initialTempo);
            }

            Refresh();
        }


        void Refresh()
        {
            if (_anywhenPlayer.AnysongObject == null) return;
            _currentPackIndex = _anywhenPlayer ? _anywhenPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnysongPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];

            _anysongPlayerControls.RefreshSongObject(_anywhenPlayer.AnysongObject);

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

#endif