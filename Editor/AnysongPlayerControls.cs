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
        private static Color _accentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
        private VisualElement _tapeElement;
        private Label _songNameLabel, _songAuthorLabel;
        private AnySlider _intensityAnySlider, _tempoAnySlider;
        private SliderInt _rootNoteSlider;
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
            _intensityAnySlider = root.Q<AnySlider>("IntensitySlider");
            _tempoAnySlider = root.Q<AnySlider>("TempoSlider");

            _rootNoteSlider = root.Q<SliderInt>("RootNoteSlider");

            _rootNoteSlider.SetValueWithoutNotify(_anywhenPlayer.GetRootNote());
            _tempoAnySlider.SetValueWithoutNotify(_anywhenPlayer.GetTempo());
            _intensityAnySlider.SetValueWithoutNotify(100);

            _intensityAnySlider.RegisterValueChangedCallback(evt => { anywhenPlayer.EditorSetTestIntensity(evt.newValue / 100f); });
            _tempoAnySlider.RegisterValueChangedCallback(evt =>
            {
                anywhenPlayer.EditorSetTempo((int)evt.newValue);
                EditorUtility.SetDirty(anywhenPlayer);
            });
            _rootNoteSlider.RegisterValueChangedCallback(evt =>
            {
                anywhenPlayer.EditorSetRootNote((int)evt.newValue);
                EditorUtility.SetDirty(anywhenPlayer);
                
            });

            _playButton.clicked += TogglePreview;
        }

        public void RefreshSongObject(AnysongObject anysongObject)
        {
            _currentSong = anysongObject;
            _songNameLabel.text = anysongObject.name;
            _songAuthorLabel.text = "By: " + anysongObject.author;
            _tempoAnySlider.SetValueWithoutNotify(_anywhenPlayer.GetTempo());
            _intensityAnySlider.SetValueWithoutNotify(100);
        }

        public void SetSongObject(AnysongObject anysongObject)
        {
            _currentSong = anysongObject;
            _songNameLabel.text = anysongObject.name;
            _songAuthorLabel.text = "By: " + anysongObject.author;
            _tempoAnySlider.SetValueWithoutNotify(anysongObject.tempo);
            _anywhenPlayer.EditorSetTempo(anysongObject.tempo);
            _intensityAnySlider.SetValueWithoutNotify(100);
        }

        private void OnTick16()
        {
            var sprite = AnywhenMetronome.Instance.Sub16 % 2 == 0 ? _tapeSprite1 : _tapeSprite2;
            _tapeElement.style.backgroundImage = new StyleBackground(sprite);
        }

        private void TogglePreview()
        {
            _isPreviewing = !_isPreviewing;
            if (_isPreviewing)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }

        public void Play()
        {
            //if (_anywhenPlayer.AnysongObject == null) return;
            if (_currentSong == null)
            {
                return;
            }

            _isPreviewing = true;
            _isPlaying = true;


            AnysongPlayerBrain.SetSectionLock(-1);
            _anywhenPlayer.EditorSetPreviewSong(_currentSong);
            AnywhenRuntime.Metronome.SetTempo(_anywhenPlayer.GetTempo());
            _playButton.style.backgroundColor = new StyleColor(_accentColor);
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