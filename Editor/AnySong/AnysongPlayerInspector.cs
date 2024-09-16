#if UNITY_EDITOR
using System;
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

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(DrawSongBrowser());
            VisualElement playButtonElement = new VisualElement();
            inspector.Add(playButtonElement);


            _playButton = new Button
            {
                text = "Preview"
            };
            _playButton.clicked += () =>
            {
                var anysongPlayer = target as AnysongPlayer;
                anysongPlayer?.ToggleEditorPreview();
                if (anysongPlayer)
                {
                    if (anysongPlayer.IsPreviewing)
                        AnywhenRuntime.Metronome.OnTick16 += OnTick16;
                    else
                    {
                        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                        _playButton.text = "Preview";
                    }
                }
            };
            playButtonElement.Add(_playButton);


            var editButton = new Button
            {
                text = "open in editor",
            };
            editButton.clicked += () =>
            {
                var anysongPlayer = target as AnysongPlayer;
                AnysongEditorWindow.ShowModuleWindow();
                AnysongEditorWindow.LoadSong(anysongPlayer?.AnysongObject);
            };
            
            inspector.Add(editButton);

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

        private void OnDestroy()
        {
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

        private AnyTrackPackObject[] _packObjects;
        private int _currentPackIndex = -1;

        VisualElement DrawSongBrowser()
        {
            VisualElement inspector = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                }
            };
            VisualElement songsList = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                }
            };


            if (_packObjects == null || _packObjects.Length == 0)
            {
                _packObjects = Resources.LoadAll<AnyTrackPackObject>("/");
            }

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
                    songsList.Clear();
                    songsList.Add(DrawSongsList());
                };
                inspector.Add(packButton);
            }


            inspector.Add(songsList);

            var nextButton = new Button
            {
                text = ">",
                style = { width = 30, }
            };
            var prevButton = new Button
            {
                text = "<",
                style = { width = 30, }
            };
            //inspector.Add(prevButton);
            //inspector.Add(nextButton);

            return inspector;
        }

        VisualElement DrawSongsList()
        {
            VisualElement songsList = new VisualElement();
            foreach (var t in _packObjects[_currentPackIndex].Songs)
            {
                var song = t;
                var songButton = new Button
                {
                    text = song.name,
                    style = { width = 200, }
                };

                var anysongObject = t;
                songButton.clicked += () =>
                {
                    var anysongPlayer = (AnysongPlayer)target;
                    anysongPlayer.SetSongObject(anysongObject);
                };
                songsList.Add(songButton);
            }

            return songsList;
        }
    }
}
#endif