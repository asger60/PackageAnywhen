using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


public class AnysongEditorWindow : EditorWindow
{
    public static AnysongObject CurrentSong { get; private set; }

    private bool _currentPatternIsBase;

    private Event _currentEvent;

    public static Color ColorHilight1 = new Color(0.4f, 0.6f, 0.5f, 1);
    public static Color ColorHilight2 = new Color(0.8f, 0.4f, 0, 1);
    public static Color ColorHilight3 = new Color(0.2f, 0.4f, 0.5f, 1);
    public static Color ColorHilight4 = new Color(0.99f, 1f, 0.9f, 1);

    public static Color ColorGreyDefault = new Color(0.35f, 0.35f, 0.35f, 1);
    public static Color ColorGreyDark = new Color(0.15f, 0.15f, 0.2f, 1);
    public static Color ColorGreyAccent = new Color(0.35f, 0.3f, 0.3f, 1);

    private static AnywhenPlayerBase _currentRuntimeSongPlayer;


    private static AnywhenPlayerBase CurrentRuntimeSongPlayer
    {
        get
        {
            if (_currentRuntimeSongPlayer == null)
            {
                var player = new GameObject("AnywhenComposerPlayer")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _currentRuntimeSongPlayer = player.AddComponent<AnywhenComposerPlayer>();
                _currentRuntimeSongPlayer.Load(CurrentSong);
                _currentRuntimeSongPlayer.SetupVoices();
                _currentRuntimeSongPlayer.LoadInstruments();
            }

            return _currentRuntimeSongPlayer;
        }
    }

    private AnysongPatternStep _stepCopy;

    private AnysongSection _sectionCopy;
    AnyPattern _patternCopy;


    public struct AnySelection
    {
        public int CurrentSectionIndex;
        public int CurrentStepIndex;
        public int CurrentPatternIndex;
        public int CurrentTrackIndex;
        public AnysongTrack CurrentSongTrack;
        public AnysongSection CurrentSection;
        public AnysongSectionTrack CurrentSectionTrack;
        public AnysongPatternStep CurrentStep;
        public AnyPattern CurrentPattern;
        public SerializedProperty CurrentSectionProperty;
        public SerializedProperty CurrentSongTrackProperty;
        public SerializedProperty CurrentSectionTrackProperty;
        public SerializedProperty CurrentStepProperty;
        public SerializedProperty CurrentPatternProperty;
    }

    private static AnySelection _currentSelection = new AnySelection();
    public static AnySelection CurrentSelection => _currentSelection;
    private AnySelection _currentHover;

    private float _scroll;

    private enum InspectorModes
    {
        Sections,
        Pattern,
        Track,
        Progression,
        Step
    }


    public static void ShowModuleWindow(AnysongObject songObject)
    {
        AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
        window.Show(true);
        window.titleContent = new GUIContent("Anysong window - " + songObject.name);
        window.minSize = new Vector2(450, 200);
        window.CreateGUI();
    }

    public static void LoadSong(AnysongObject songObject)
    {
        var player = new GameObject("AnywhenComposerPlayer")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var composerPlayer = player.AddComponent<AnywhenComposerPlayer>();
        composerPlayer.Load(songObject);

        _currentRuntimeSongPlayer = composerPlayer;
        CurrentSong = songObject;
        _currentRuntimeSongPlayer.SetupVoices();

        _currentRuntimeSongPlayer.LoadInstruments();
        EditorPrefs.SetString("AnyLoadedSong", AssetDatabase.GetAssetPath(songObject));
        AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
        AnywhenRuntime.Log("Loaded: " + AssetDatabase.GetAssetPath(songObject));

        // Set the default UXML template if not already set
        if (!window.uxmlAsset)
            window.uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongEditorWindow.uxml");


        if (!window.styleAsset)
            window.styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/PackageAnywhen/Editor/USS/AnywhenEditorStyles.uss");

        window.Show(true);

        foreach (var track in songObject.Tracks)
        {
            if (track.trackEnvelope.IsUnset())
            {
                track.trackEnvelope.Initialize();
            }
        }

        window.CreateGUI();
    }

    private static bool _isPLaying;

    private static void ToggleIsPlaying()
    {
        _isPLaying = !_isPLaying;

        if (_isPLaying)
        {
            AnywhenRuntime.SetPreviewMode(true, CurrentRuntimeSongPlayer);
            AnysongSectionsView.RefreshSectionLocked();
            AnywhenMetronome.Instance.SetTempo(CurrentSong.tempo);
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            AnywhenRuntime.Metronome.OnNextBar += OnBar;
            OnBar();
        }
        else
        {
            AnywhenRuntime.SetPreviewMode(false, CurrentRuntimeSongPlayer);
            CurrentSong.Reset();
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            AnywhenRuntime.Metronome.OnNextBar -= OnBar;
            AnysongSectionsView.HilightSection(-1, _currentSelection.CurrentSectionIndex);
            AnysongStepsView.ResetTriggered();
            AnysongProgressionsView.ResetTriggered();
        }
    }

    private void OnDestroy()
    {
        AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        AnywhenRuntime.Metronome.OnNextBar -= OnBar;
    }

    static void SetTestIntensity(float value)
    {
        if (_isPLaying)
        {
            CurrentRuntimeSongPlayer.SetIntensity(value);
        }
    }


    private VisualElement _mainViewPanel;
    private VisualElement _transportPanel;
    private VisualElement _sequencesPanel;
    private VisualElement _sectionsPanel;
    private VisualElement _tracksPanel;
    private VisualElement _inspectorPanel;
    private VisualElement _progressionPanel;
    [SerializeField] VisualTreeAsset uxmlAsset;
    [SerializeField] StyleSheet styleAsset;

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        if (!CurrentSong)
        {
            //Debug.Log("Current song is null");
            rootVisualElement.Add(new Label("No song loaded"));
            return;
        }

        if (uxmlAsset)
        {
            uxmlAsset.CloneTree(rootVisualElement);
        }
        else
        {
            rootVisualElement.Add(new Label("failed to load uxml"));
            return;
        }

        if (styleAsset)
            rootVisualElement.styleSheets.Add(styleAsset);
        else
            rootVisualElement.Add(new Label("failed to load uss"));

        _currentSelection = new AnySelection
        {
            CurrentPatternIndex = 0,
            CurrentStepIndex = 0,
            CurrentSection = CurrentSong.Sections[0]
        };

        if (CurrentSong.Sections.Count == 0)
        {
            CreateNewSection();
        }


        if (CurrentSong.Tracks.Count == 0)
        {
            CreateNewTrack();
        }

        _transportPanel = rootVisualElement.Q<VisualElement>("Transport");
        _sectionsPanel = rootVisualElement.Q<VisualElement>("SectionsContainer");
        _tracksPanel = rootVisualElement.Q<VisualElement>("TracksContainer");
        _progressionPanel = rootVisualElement.Q<VisualElement>("PatternsContainer");
        _inspectorPanel = rootVisualElement.Q<VisualElement>("InspectorContainer");
        _sequencesPanel = rootVisualElement.Q<VisualElement>("SequencesContainer");


        AnysongTransportView.Draw(_transportPanel, CurrentSong);
        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
        AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
        AnysongStepsView.Draw(_sequencesPanel);
        AnysongInspectorView.Draw(_inspectorPanel);

        HandleTransportLogic();
        HandleSectionsLogic();
        HandleTracksLogic();
        HandleSequencesLogic();
        RegisterKeyboardInputs();
        RegisterScrollWheelInputs();
        HandleProgressionLogic();
    }

    void HandleTransportLogic()
    {
        _transportPanel.Q<Button>("PlayButton").RegisterCallback<ClickEvent>((evt) =>
        {
            ToggleIsPlaying();
            AnysongTransportView.RefreshPlaybuttonState(_isPLaying);
        });

        _transportPanel.Q<Slider>("TestIntensitySlider").RegisterValueChangedCallback(evt => { SetTestIntensity(evt.newValue); });
    }

    private static void OnTick16()
    {
        if (_currentSelection.CurrentSection == null)
            _currentSelection.CurrentSection = CurrentSong.Sections[0];
        if (_currentSelection.CurrentSectionIndex == CurrentRuntimeSongPlayer.CurrentSectionIndex)
        {
            for (var i = 0; i < CurrentSong.Tracks.Count; i++)
            {
                AnysongStepsView.HilightStepIndex(i,
                    _currentSelection.CurrentSection.tracks[i].GetSelectedPatternIndex() ==
                    _currentSelection.CurrentSection.tracks[i].GetPlayingPatternIndex());
            }
        }
    }

    static void OnBar()
    {
        if (CurrentSong != CurrentRuntimeSongPlayer.CurrentSong) return;
        AnysongSectionsView.HilightSection(CurrentRuntimeSongPlayer.CurrentSectionIndex, _currentSelection.CurrentSectionIndex);

        var ints = CurrentRuntimeSongPlayer.EditorGetPlayingTrackPatternIndexes();
        for (var i = 0; i < ints.Length; i++)
        {
            var patternIndex = ints[i];
            AnysongProgressionsView.HilightPattern(i, patternIndex, _currentSelection.CurrentPatternIndex);
        }
    }


    void RegisterKeyboardInputs()
    {
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
    }

    void RegisterScrollWheelInputs()
    {
        rootVisualElement.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
        rootVisualElement.focusable = true;
        rootVisualElement.pickingMode = PickingMode.Position;
        rootVisualElement.Focus();
    }


    private void OnWheel(WheelEvent evt)
    {
        if (_currentHover.CurrentStep == null) return;
        _scroll += evt.delta.y;
        _currentHover.CurrentStep.rootNote = (int)(_scroll * 1.2f);
        AnysongStepsView.RefreshPatterns();
    }


    private void OnKeyDown(KeyDownEvent evt)
    {
        //Debug.Log("pressed " + evt.keyCode);
        if (_currentHover.CurrentStep == null) return;
        if (evt.keyCode == KeyCode.C)
        {
            if (_currentHover.CurrentStep != null)
            {
                CopyStep(_currentHover.CurrentStep);
            }
            else if (_currentSelection.CurrentStep != null)
            {
                CopyStep(_currentSelection.CurrentStep);
            }
        }

        if (evt.keyCode == KeyCode.V)
        {
            if (_currentHover.CurrentStep != null && _stepCopy != null)
            {
                PasteStep(_currentHover.CurrentPattern, _currentHover.CurrentStep);
            }
            else if (_currentSelection.CurrentStep != null && _stepCopy != null)
            {
                PasteStep(_currentSelection.CurrentPattern, _currentSelection.CurrentStep);
            }
        }

        if (evt.keyCode == KeyCode.UpArrow)
        {
            _currentHover.CurrentStep.rootNote++;
            AnysongStepsView.RefreshPatterns();
        }

        if (evt.keyCode == KeyCode.DownArrow)
        {
            _currentHover.CurrentStep.rootNote--;
            AnysongStepsView.RefreshPatterns();
        }
    }

    void HandleSectionsLogic()
    {
        _sectionsPanel.Query<Button>("SectionButton").ForEach(button =>
        {
            button.RegisterCallback((ClickEvent ev) =>
            {
                if (ev.currentTarget is Button btn)
                {
                    _currentSelection.CurrentPatternIndex = 0;
                    _currentSelection = GetSectionFromTooltip(btn.tooltip);

                    SetInspectorMode(InspectorModes.Sections);
                    AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
                    AnysongStepsView.Draw(_sequencesPanel);
                    AnysongStepsView.RefreshPatterns();
                    HandleSequencesLogic();
                    //HandlePatternsLogic();
                    RefreshSectionLockIndex();
                    HandleSectionsLogic();
                    HandleProgressionLogic();

                    AnysongTracksView.UpdateMuteSoleState();
                    AnysongSectionsView.HilightSection(CurrentRuntimeSongPlayer.CurrentSectionIndex, _currentSelection.CurrentSectionIndex);
                }
            });
        });

        _sectionsPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreateNewSection(); });
        _sectionsPanel.Q<Button>("RemoveButton").RegisterCallback((ClickEvent ev) => { DeleteSection(); });
        _sectionsPanel.Q<Button>("SectionLockButton").RegisterCallback((ClickEvent ev) => { ToggleSectionLock(); });
    }


    void HandleTracksLogic()
    {
        _tracksPanel.Query<Button>("TrackButton").ForEach(button =>
        {
            button.RegisterCallback((ClickEvent ev) =>
            {
                if (ev.currentTarget is Button btn)
                {
                    _currentSelection = GetSelectionFromTooltip(btn.tooltip);
                    SetInspectorMode(InspectorModes.Track);
                }
            });
        });

        _tracksPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreateNewTrack(); });
        _tracksPanel.Q<Button>("RemoveButton").RegisterCallback((ClickEvent ev) => { DeleteTrack(); });
    }


    void HandleProgressionLogic()
    {
        _progressionPanel.Query<Button>("ProgressionButton").ForEach(button =>
        {
            button.RegisterCallback((ClickEvent ev) =>
            {
                if (ev.currentTarget is Button btn)
                {
                    _currentSelection = GetSelectionFromTooltip(btn.tooltip);
                    SetInspectorMode(InspectorModes.Progression);
                }
            });
        });
        _progressionPanel.Query<Button>("AddButton").ForEach(button =>
        {
            button.RegisterCallback((ClickEvent ev) =>
            {
                if (ev.currentTarget is Button btn)
                {
                    CreatePattern(Int32.Parse(btn.tooltip));
                    AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
                    AnysongProgressionsView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex, _currentSelection.CurrentPatternIndex);
                    HandleProgressionLogic();
                    AnysongStepsView.RefreshPatterns();
                }
            });
        });

        _progressionPanel.Query<Button>("PatternButton").ForEach((button) =>
        {
            button.RegisterCallback<ClickEvent>((evt) =>
            {
                if (evt.currentTarget is not Button btn) return;
                _currentSelection = GetSelectionFromTooltip(btn.tooltip);
                AnysongProgressionsView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex, _currentSelection.CurrentPatternIndex);
                AnysongStepsView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex, _currentSelection.CurrentPatternIndex);
                AnysongStepsView.RefreshPatterns();

                _currentPatternIsBase = _currentSelection.CurrentPatternIndex == 0;
                SetInspectorMode(InspectorModes.Pattern);
            });
        });
    }

    void CreatePattern(int trackIndex)
    {
        Debug.Log("create new pattern");

        var newPattern = new AnyPattern();
        newPattern.Init();
        var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

        thisTrack.patterns.Add(newPattern);
        _currentSelection.CurrentPatternIndex = thisTrack.patterns.Count - 1;
        thisTrack.SetSelectedPattern(thisTrack.patterns.Count - 1);

        AnysongStepsView.Draw(_sequencesPanel);
        AnysongStepsView.RefreshPatterns();
        HandleSequencesLogic();
        //HandlePatternsLogic();
        HandleProgressionLogic();
    }


    void DeletePattern(int trackIndex)
    {
        Debug.Log("remove pattern");

        var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

        thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);
        _currentSelection.CurrentPatternIndex = thisTrack.patterns.Count - 1;
        thisTrack.SetSelectedPattern(thisTrack.patterns.Count - 1);
        AnysongStepsView.Draw(_sequencesPanel);
        AnysongStepsView.RefreshPatterns();
        AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
        HandleSequencesLogic();
        //HandlePatternsLogic();
        HandleProgressionLogic();
    }

    void CreateNewTrack()
    {
        var newTrack = new AnysongTrack();
        newTrack.Init();
        CurrentSong.Tracks.Add(newTrack);
        foreach (var section in CurrentSong.Sections)
        {
            section.AddSongTrack(newTrack);
        }

        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
        AnysongStepsView.Draw(_sequencesPanel);
        HandleSequencesLogic();
        HandleTracksLogic();

        EditorUtility.SetDirty(CurrentSong);
    }

    void DeleteTrack()
    {
        foreach (var section in CurrentSong.Sections)
        {
            section.RemoveSongTrack(_currentSelection.CurrentTrackIndex);
        }

        CurrentSong.Tracks.RemoveAt(_currentSelection.CurrentTrackIndex);
        _currentSelection.CurrentTrackIndex = 0;

        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
        AnysongStepsView.Draw(_sequencesPanel);
        HandleSequencesLogic();
        HandleTracksLogic();
        EditorUtility.SetDirty(CurrentSong);
        Debug.Log("delete track");
    }

    void CreateNewSection()
    {
        Debug.Log("create new section");
        var newSection = new AnysongSection();
        newSection.Init(CurrentSong.Tracks);
        CurrentSong.Sections.Add(newSection);
        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        HandleSectionsLogic();
    }

    void DeleteSection()
    {
        CurrentSong.Sections.RemoveAt(_currentSelection.CurrentSectionIndex);
        _currentSelection.CurrentSectionIndex = CurrentSong.Sections.Count - 1;

        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
        AnysongStepsView.Draw(_sequencesPanel);
        HandleSequencesLogic();
        HandleTracksLogic();
        HandleSectionsLogic();
    }

    void CopySection()
    {
        Debug.Log("CopySection");
        _sectionCopy = _currentSelection.CurrentSection;
        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        HandleSectionsLogic();
    }

    void PasteSection()
    {
        Debug.Log("PasteSection");
        CurrentSong.Sections[_currentSelection.CurrentSectionIndex] = _sectionCopy.Clone();
        AnysongStepsView.RefreshPatterns();
        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        HandleSectionsLogic();
    }

    public static int CurrentSectionLockIndex;

    public static AnysongSection GetCurrentSection()
    {
        if (CurrentSectionLockIndex > -1)
            return CurrentSong.Sections[CurrentSectionLockIndex];

        return CurrentSong.Sections[0];
    }

    void RefreshSectionLockIndex()
    {
        if (!AnysongSectionsView.IsSectionLocked()) return;

        CurrentSectionLockIndex = _currentSelection.CurrentSectionIndex;
        Debug.Log("set locked section to  " + _currentSelection.CurrentSectionIndex);
        AnysongPlayerBrain.SetSectionLock(CurrentSectionLockIndex);
    }

    public static AnySelection GetCurrentSelection()
    {
        return _currentSelection;
    }

    void ToggleSectionLock()
    {
        if (CurrentSectionLockIndex >= 0)
        {
            CurrentSectionLockIndex = -1;
            AnysongPlayerBrain.SetSectionLock(CurrentSectionLockIndex);
        }
        else
        {
            CurrentSectionLockIndex = _currentSelection.CurrentSectionIndex;
            AnysongPlayerBrain.SetSectionLock(CurrentSectionLockIndex);
        }

        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
        HandleSectionsLogic();
    }


    void HandleSequencesLogic()
    {
        _sequencesPanel.Query<Button>("StepButton").ForEach(button =>
        {
            button.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnterEvent, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent, TrickleDown.TrickleDown);
        });
    }


    private void OnPointerEnterEvent(PointerEnterEvent evt)
    {
        if (evt.currentTarget is not Button btn) return;
        _currentHover = GetSelectionFromTooltip(btn.tooltip);
        _scroll = _currentHover.CurrentStep.rootNote;
    }


    private void OnPointerLeaveEvent(PointerLeaveEvent evt)
    {
        if (evt.currentTarget is not Button btn) return;
        _currentHover.CurrentStep = null;
        _currentHover.CurrentPattern = null;
        _currentHover.CurrentSectionTrack = null;
    }

    private void OnPointerDownEvent(PointerDownEvent evt)
    {
        if (evt.currentTarget is not Button btn) return;
        _currentSelection = GetSelectionFromTooltip(btn.tooltip);

        SetInspectorMode(InspectorModes.Step);
        if (evt.button == 0)
        {
            _currentSelection.CurrentStep.noteOn = !_currentSelection.CurrentStep.noteOn;
        }
    }

    AnySelection GetSectionFromTooltip(string tooltip)
    {
        var str = tooltip.Split("-");
        var selection = new AnySelection
        {
            CurrentSectionIndex = Int32.Parse(str[0]),
        };
        _currentSelection.CurrentSectionIndex = selection.CurrentSectionIndex;
        _currentSelection.CurrentSection = CurrentSong.Sections[_currentSelection.CurrentSectionIndex];
        _currentSelection.CurrentSectionTrack =
            _currentSelection.CurrentSection.tracks[_currentSelection.CurrentTrackIndex];
        _currentSelection.CurrentPattern =
            _currentSelection.CurrentSectionTrack.GetPattern(_currentSelection.CurrentPatternIndex);
        _currentSelection.CurrentStep = _currentSelection.CurrentPattern.steps[_currentSelection.CurrentStepIndex];
        _currentSelection.CurrentSongTrack = CurrentSong.Tracks[_currentSelection.CurrentTrackIndex];


        var song = new SerializedObject(CurrentSong);
        var section = song.FindProperty("Sections").GetArrayElementAtIndex(_currentSelection.CurrentSectionIndex);
        var track = section.FindPropertyRelative("tracks")
            .GetArrayElementAtIndex(_currentSelection.CurrentTrackIndex);
        var pattern = track.FindPropertyRelative("patterns")
            .GetArrayElementAtIndex(_currentSelection.CurrentPatternIndex);
        var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(_currentSelection.CurrentStepIndex);

        _currentSelection.CurrentSectionProperty = section;
        _currentSelection.CurrentPatternProperty = pattern;
        _currentSelection.CurrentStepProperty = step;
        _currentSelection.CurrentSongTrackProperty = song.FindProperty("Tracks")
            .GetArrayElementAtIndex(_currentSelection.CurrentTrackIndex);
        _currentSelection.CurrentSectionTrackProperty = track;

        return _currentSelection;
    }

    AnySelection GetSelectionFromTooltip(string tooltip)
    {
        var str = tooltip.Split("-");

        var selection = new AnySelection
        {
            CurrentStepIndex = Int32.Parse(str[0]),
            CurrentTrackIndex = Int32.Parse(str[1]),
            CurrentPatternIndex = Int32.Parse(str[2]),
            CurrentSectionIndex = _currentSelection.CurrentSectionIndex,
            CurrentSection = _currentSelection.CurrentSection,
        };

        _currentSelection.CurrentSection ??= CurrentSong.Sections[_currentSelection.CurrentSectionIndex];

        selection.CurrentSectionTrack = _currentSelection.CurrentSection.tracks[selection.CurrentTrackIndex];
        selection.CurrentPattern = selection.CurrentSectionTrack.patterns[selection.CurrentPatternIndex];
        selection.CurrentStep = selection.CurrentPattern.steps[selection.CurrentStepIndex];
        selection.CurrentSongTrack = CurrentSong.Tracks[selection.CurrentTrackIndex];

        var song = new SerializedObject(CurrentSong);

        var section = song.FindProperty("Sections").GetArrayElementAtIndex(selection.CurrentSectionIndex);
        var track = section.FindPropertyRelative("tracks").GetArrayElementAtIndex(selection.CurrentTrackIndex);
        var pattern = track.FindPropertyRelative("patterns").GetArrayElementAtIndex(selection.CurrentPatternIndex);
        var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(selection.CurrentStepIndex);

        selection.CurrentPatternProperty = pattern;
        selection.CurrentStepProperty = step;
        selection.CurrentSongTrackProperty =
            song.FindProperty("Tracks").GetArrayElementAtIndex(selection.CurrentTrackIndex);

        selection.CurrentSectionProperty = section;
        selection.CurrentSectionTrackProperty = track;

        return selection;
    }


    public static AnyPattern GetCurrentPlayingPatternForTrack(int trackIndex)
    {
        return CurrentSong.Sections[CurrentRuntimeSongPlayer.CurrentSectionIndex].tracks[trackIndex]
            .GetPlayingPattern();
    }

    public static AnyPattern GetPatternFromTooltip(string tooltip)
    {
        var str = tooltip.Split("-");
        int stepIndex = Int32.Parse(str[0]);
        int trackIndex = Int32.Parse(str[1]);
        int patternIndex = Int32.Parse(str[2]);

        return CurrentSong.Sections[_currentSelection.CurrentSectionIndex].tracks[trackIndex]
            .patterns[patternIndex];
    }

    public static AnysongPatternStep GetPatternStepFromTooltip(string tooltip)
    {
        var str = tooltip.Split("-");
        int stepIndex = Int32.Parse(str[0]);
        int trackIndex = Int32.Parse(str[1]);
        int patternIndex = Int32.Parse(str[2]);

        return CurrentSong.Sections[_currentSelection.CurrentSectionIndex].tracks[trackIndex].patterns[patternIndex]
            .steps[stepIndex];
    }

    void SetInspectorMode(InspectorModes inspectorMode)
    {
        switch (inspectorMode)
        {
            case InspectorModes.Sections:
                AnysongInspectorView.DrawSection(_currentSelection.CurrentSectionProperty, false, null);

                _inspectorPanel.Q<Button>("CopyButton").RegisterCallback((ClickEvent ev) => { CopySection(); });
                _inspectorPanel.Q<Button>("PasteButton").RegisterCallback((ClickEvent ev) => { PasteSection(); });
                break;
            case InspectorModes.Pattern:
                AnysongInspectorView.DrawPattern(_currentSelection.CurrentPatternProperty, _currentPatternIsBase,
                    AnysongStepsView.RefreshPatterns);
                _inspectorPanel.Q<Button>("DeleteButton").RegisterCallback((ClickEvent ev) =>
                {
                    DeletePattern(_currentSelection.CurrentTrackIndex);
                });
                _inspectorPanel.Q<Button>("ScrubForward")
                    .RegisterCallback((ClickEvent ev) => { ScrubPattern(-1); });
                _inspectorPanel.Q<Button>("ScrubBack").RegisterCallback((ClickEvent ev) => { ScrubPattern(1); });

                _inspectorPanel.Q<Button>("RandomizeMelody").RegisterCallback((ClickEvent ev) => { RandomizeMelody(); });
                _inspectorPanel.Q<Button>("RandomizeRhythm").RegisterCallback((ClickEvent ev) => { RandomizeRhythm(); });


                HandlePatternUtilsLogic();
                break;
            case InspectorModes.Track:
                AnysongInspectorView.DrawTrack(_currentSelection,
                    () =>
                    {
                        CurrentRuntimeSongPlayer.UpdateTrackInstrument(_currentSelection.CurrentSongTrack);
                    });

                break;
            case InspectorModes.Step:
                AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty,
                    AnysongStepsView.RefreshPatterns);
                HandleStepUtilsLogic();
                break;
            case InspectorModes.Progression:
                AnysongInspectorView.DrawProgression(_currentSelection);
                _inspectorPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreatePattern(_currentSelection.CurrentTrackIndex); });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(inspectorMode), inspectorMode, null);
        }
    }


    void RandomizeMelody()
    {
        _currentSelection.CurrentPattern.RandomizeMelody();
        AnysongStepsView.RefreshPatterns();
    }

    void RandomizeRhythm()
    {
        _currentSelection.CurrentPattern.RandomizeRhythm();
        AnysongStepsView.RefreshPatterns();
    }

    void ScrubPattern(int direction)
    {
        _currentSelection.CurrentPattern.Scrub(direction);
        AnysongStepsView.RefreshPatterns();
    }


    void HandlePatternUtilsLogic()
    {
        _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) => { CopyPattern(_currentSelection.CurrentPattern); });
        _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) => { PastePattern(); });
    }

    void HandleStepUtilsLogic()
    {
        _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) => { CopyStep(_currentSelection.CurrentStep); });
        _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) =>
        {
            PasteStep(_currentSelection.CurrentPattern, _currentSelection.CurrentStep);
        });
    }


    void CopyStep(AnysongPatternStep step)
    {
        Debug.Log("Copy step");
        _stepCopy = step.Clone();
    }

    void PasteStep(AnyPattern pattern, AnysongPatternStep step)
    {
        Debug.Log("paste step");
        for (var i = 0; i < pattern.steps.Count; i++)
        {
            var patternStep = pattern.steps[i];
            if (patternStep == step)
            {
                pattern.steps[i] = _stepCopy;
                break;
            }
        }

        AnysongStepsView.RefreshPatterns();
        _stepCopy = _stepCopy.Clone();
    }

    public static AnysongEditorWindow Create(Object asset)
    {
        var window = CreateWindow<AnysongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
        return window;
    }


    private string _consoleMessage;
    private double _consoleLogTime;


    void HandleConsole()
    {
        GUILayout.FlexibleSpace();
        GUI.color = Color.green;
        GUILayout.Label(_consoleMessage);
        GUI.color = Color.white;
        if (EditorApplication.timeSinceStartup > _consoleLogTime + 3)
        {
            _consoleMessage = "";
        }
    }

    void ConsoleLog(string message)
    {
        _consoleMessage = message;
        _consoleLogTime = EditorApplication.timeSinceStartup;
    }


    void ClearPattern()
    {
        var newPattern = new AnyPattern();
        newPattern.Init();
        //CurrentSectionTrack.EditorCurrentPattern = newPattern;
    }

    void CopyPattern(AnyPattern pattern)
    {
        Debug.Log("Copy pattern");
        AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
        _patternCopy = pattern.Clone();
        window.ConsoleLog("copied pattern");
    }

    void PastePattern()
    {
        if (_patternCopy == null) return;
        for (var i = 0; i < _currentSelection.CurrentSectionTrack.patterns.Count; i++)
        {
            if (_currentSelection.CurrentSectionTrack.patterns[i] == _currentSelection.CurrentPattern)
            {
                _currentSelection.CurrentSectionTrack.patterns[i] = _patternCopy;
                break;
            }
        }

        _patternCopy = _patternCopy.Clone();
    }

    public static VisualElement CreateAddRemoveButtons(bool addButton = true, bool removeButton = true)
    {
        var buttons = new VisualElement
        {
            style =
            {
                alignItems = Align.Center,
                flexDirection = FlexDirection.Row
            }
        };

        if (addButton)
        {
            var newAddButton = new Button
            {
                name = "AddButton",
                text = "+"
            };
            buttons.Add(newAddButton);
        }

        if (removeButton)
        {
            var deleteButton = new Button
            {
                name = "RemoveButton",
                text = "-"
            };

            buttons.Add(deleteButton);
        }

        return buttons;
    }
}