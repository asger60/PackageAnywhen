using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class AnysongPlayerControls
{
    private Sprite _tapeSprite1;
    private Sprite _tapeSprite2;
    private bool _isPreviewing;
    private Button _playButton, _editButton;

    private AnywhenPlayer _anywhenPlayer;
    private AnysongObject _currentSong;
    private static Color _accentColor = new Color(0.3764705882f, 0.7803921569f, 0.3607843137f, 1);
    private VisualElement _tapeElement;
    private Label _songNameLabel, _songAuthorLabel;


    // private SliderInt _rootNoteSlider;
    private List<Button> _sectionButtons = new List<Button>();
    private bool _isPlaying;

    static AnysongPlayerControls()
    {
    }

    public bool IsPlaying => _isPlaying;

    public void HandlePlayerLogic(VisualElement root, AnywhenPlayer anywhenPlayer)
    {
        _anywhenPlayer = anywhenPlayer;
        _tapeSprite1 =
            AssetDatabase.LoadAssetAtPath<Sprite>(AnywhenMenuUtils.GetAssetPath("Editor/Sprites/Tape1.png"));
        _tapeSprite2 =
            AssetDatabase.LoadAssetAtPath<Sprite>(AnywhenMenuUtils.GetAssetPath("Editor/Sprites/Tape2.png"));

        _tapeElement = root.Q<VisualElement>("TapeElement");
        _playButton = root.Q<Button>("ButtonPreview");
        _songNameLabel = root.Q<Label>("LabelSongTitle");
        _songAuthorLabel = root.Q<Label>("LabelSongAuthor");


        _editButton = root.Q<Button>("ButtonEdit");


        var sectionButtonsElement = root.Q<VisualElement>("SectionButtonsElement");

        sectionButtonsElement.Query<Button>("SectionButton")
            .ForEach(button => { sectionButtonsElement.Remove(button); });

        _sectionButtons.Clear();
        if (anywhenPlayer.AnysongObject != null)
        {
            for (var i = 0; i < anywhenPlayer.AnysongObject.Sections.Count; i++)
            {
                var btn = new Button
                {
                    text = i.ToString()
                };
                btn.AddToClassList("section-button");
                btn.clicked += () => { _anywhenPlayer.EditorSetSection(Int32.Parse(btn.text)); };
                _sectionButtons.Add(btn);
                sectionButtonsElement.Add(btn);
            }
        }
        else
        {
            sectionButtonsElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        if (_currentSong == null)
        {
            var dimColor = new StyleColor(new Color(1, 1, 1, 0.1f));

            _playButton.style.unityBackgroundImageTintColor = dimColor;
            _tapeElement.style.unityBackgroundImageTintColor = dimColor;
            _editButton.style.unityBackgroundImageTintColor = dimColor;
        }

        _playButton.clicked += TogglePreview;
    }

    public void RefreshSongObject(AnysongObject anysongObject)
    {
        _currentSong = anysongObject;
        _songNameLabel.text = anysongObject.name;
        _songAuthorLabel.text = "By: " + anysongObject.author;


        _playButton.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, 1));
        _tapeElement.style.unityBackgroundImageTintColor = Color.white;
        _editButton.style.unityBackgroundImageTintColor = Color.white;
    }

    void RefreshActiveSection()
    {
        for (int i = 0; i < _sectionButtons.Count; i++)
        {
            _sectionButtons[i].style.backgroundColor =
                new StyleColor((i == _anywhenPlayer.CurrentSong.CurrentSectionIndex) ? Color.grey : Color.clear);
        }
    }

    public void SetSongObject(AnysongObject anysongObject)
    {
        _currentSong = anysongObject;
        _songNameLabel.text = anysongObject.name;
        _songAuthorLabel.text = "By: " + anysongObject.author;
        _anywhenPlayer.EditorSetTempo(anysongObject.tempo);
        _playButton.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, 1));
        _tapeElement.style.unityBackgroundImageTintColor = Color.white;
        _editButton.style.unityBackgroundImageTintColor = Color.white;
    }

    private void OnTick16()
    {
        var sprite = AnywhenMetronome.Instance.Sub16 % 2 == 0 ? _tapeSprite1 : _tapeSprite2;
        _tapeElement.style.backgroundImage = new StyleBackground(sprite);
        RefreshActiveSection();
    }

    private void TogglePreview()
    {
        if (_anywhenPlayer == null) return;
        if (_currentSong == null) return;
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
        if (_anywhenPlayer == null) return;
        if (_currentSong == null) return;
        _anywhenPlayer.LoadInstruments();
        _isPreviewing = true;
        _isPlaying = true;


        _currentSong.Play(AnysongObject.SongPlayModes.Playback);
        //AnysongPlayerBrain.SetSectionLock(-1);
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