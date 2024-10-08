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
        private AnysongPlayer _anysongPlayer;
        private AnysongObject _currentSong;
        public static Color AccentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private VisualElement _tapeElement;


        static AnysongPlayerControls()
        {
        }

        public void HandlePlayerLogic(VisualElement root, AnysongPlayer anysongPlayer)
        {
            _anysongPlayer = anysongPlayer;
            _tapeSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PackageAnywhen/Editor/Sprites/Tape1.png");
            _tapeSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PackageAnywhen/Editor/Sprites/Tape2.png");

            _tapeElement = root.Q<VisualElement>("TapeElement");
            _playButton = root.Q<Button>("ButtonPreview");
            _playButton.clicked += TogglePreview;
        }

        public void SetSongObject(AnysongObject anysongObject)
        {
            _currentSong = anysongObject;
            Debug.Log("song " +_currentSong.name);
        }

        private void OnTick16()
        {
            var sprite = AnywhenMetronome.Instance.Sub16 % 2 == 0 ? _tapeSprite1 : _tapeSprite2;
            _tapeElement.style.backgroundImage = new StyleBackground(sprite);
        }

        private void TogglePreview()
        {
            _isPreviewing = !_isPreviewing;
            Debug.Log("preview" + _isPreviewing);
            if (_isPreviewing)
            {
                if (_currentSong == null)
                {
                    _currentSong = _anysongPlayer.AnysongObject;
                }

                _anysongPlayer.SetPreviewSong(_currentSong);

                AnywhenRuntime.Metronome.SetTempo(_currentSong.tempo);
                _playButton.style.backgroundColor = new StyleColor(AccentColor);
                AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            }
            else
            {
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                _playButton.style.backgroundColor = new StyleColor(Color.clear);
            }

            AnywhenRuntime.SetPreviewMode(_isPreviewing, _anysongPlayer);
        }
    }
}