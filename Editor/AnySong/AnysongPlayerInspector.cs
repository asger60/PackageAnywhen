#if UNITY_EDITOR
using System;
using System.Linq;
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
        private Button _playButton;
        private AnysongPlayer _anysongPlayer;
        private VisualElement _currentPackButtonHolder;
        private VisualElement _currentSongButtonHolder;
        private VisualElement _packArtHolder;
        private AnyTrackPackObject[] _packObjects;
        private int _currentPackIndex = -1;
        private AnyTrackPackObject _currentPack;
        private Image _packArtImage;

        private void OnEnable()
        {
            _anysongPlayer = target as AnysongPlayer;
            _currentPackIndex = _anysongPlayer ? _anysongPlayer.currentSongPackIndex : 0;
            _packObjects = Resources.LoadAll<AnyTrackPackObject>("/");
            _currentPack = _packObjects[_currentPackIndex];
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();


            inspector.Add(DrawPackGraphics());


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

            AnywhenRuntime.TogglePreviewMode(anysongPlayer);
            //anysongPlayer.ToggleEditorPreview();
            AnywhenRuntime.Metronome.SetTempo(anysongPlayer.AnysongObject.tempo);


            if (AnywhenRuntime.IsPreviewing)
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


        VisualElement DrawPackGraphics()
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

            var browserButton = new Button
            {
                text = "Open song browser"
            };
            browserButton.clicked += () => { AnySongBrowser.ShowBrowserWindow(_anysongPlayer); };

          
            inspector.Add(_packArtHolder);
            inspector.Add(browserButton);
            return inspector;
        }

       
    }
}
#endif