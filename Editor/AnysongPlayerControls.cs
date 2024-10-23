using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class AnysongPlayerControls
    {
        private Sprite _tapeSprite1;
        private Sprite _tapeSprite2;
        private bool _isPreviewing;
        private Button _playButton;
        private AnywhenPlayer _anywhenPlayer;
        private AnysongObject _currentSong;
        public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private VisualElement _tapeElement;
        private Label _songNameLabel, _songAuthorLabel;
        private bool _isPlaying;

        static AnysongPlayerControls()
        {
        }

        public bool IsPlaying => _isPlaying;

        public void HandlePlayerLogic(VisualElement root, AnywhenPlayer anywhenPlayer)
        {
            _anywhenPlayer = anywhenPlayer;
            _tapeSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>(AnywhenMenuUtils.GetAssetPath("Editor/Sprites/Tape1.png"));
            _tapeSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>(AnywhenMenuUtils.GetAssetPath("Editor/Sprites/Tape2.png"));

            _tapeElement = root.Q<VisualElement>("TapeElement");
            _playButton = root.Q<Button>("ButtonPreview");
            _songNameLabel = root.Q<Label>("LabelSongTitle");
            _songAuthorLabel = root.Q<Label>("LabelSongAuthor");

            _playButton.clicked += TogglePreview;
        }

        public void SetSongObject(AnysongObject anysongObject)
        {
            _currentSong = anysongObject;
            _songNameLabel.text = anysongObject.name;
            _songAuthorLabel.text = "By: " + anysongObject.author;
        }

        private void OnTick16()
        {
            var sprite = AnywhenMetronome.Instance.Sub16 % 2 == 0 ? _tapeSprite1 : _tapeSprite2;
            _tapeElement.style.backgroundImage = new StyleBackground(sprite);
        }

        private void TogglePreview()
        {
            if (_anywhenPlayer.AnysongObject == null) return;
            _isPreviewing = !_isPreviewing;
            if(_isPreviewing)
            {
                Play();
            }
            else
            {
                Stop();
            }


            //_isPlaying = _isPreviewing;
            //if (_isPreviewing)
            //{
            //    if (_currentSong == null)
            //    {
            //        _currentSong = _anywhenPlayer.AnysongObject;
            //    }
//
            //    _anywhenPlayer.EditorSetPreviewSong(_currentSong);
//
            //    AnywhenRuntime.Metronome.SetTempo(_currentSong.tempo);
            //    _playButton.style.backgroundColor = new StyleColor(AccentColor);
            //    AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            //}
            //else
            //{
            //    AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            //    _playButton.style.backgroundColor = new StyleColor(Color.clear);
            //}

            
        }

        public void Play()
        {
            if (_anywhenPlayer.AnysongObject == null) return;
            
            _isPreviewing = true;
            _isPlaying = true;
            if (_currentSong == null)
            {
                _currentSong = _anywhenPlayer.AnysongObject;
            }

            _anywhenPlayer.EditorSetPreviewSong(_currentSong);

            AnywhenRuntime.Metronome.SetTempo(_currentSong.tempo);
            _playButton.style.backgroundColor = new StyleColor(AccentColor);
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.SetPreviewMode(_isPreviewing, _anywhenPlayer);
        }
        public void Stop()
        {
            _isPlaying = false;
            _isPreviewing = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            _playButton.style.backgroundColor = new StyleColor(Color.clear);
            AnywhenRuntime.SetPreviewMode(_isPreviewing, _anywhenPlayer);
        }
    }
}