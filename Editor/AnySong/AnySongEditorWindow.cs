using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace Anysong
{
    public class AnysongEditorWindow : EditorWindow
    {
        public static AnysongObject CurrentSong { get; private set; }

        private bool _currentPatternIsBase;

        private Event _currentEvent;


        public static Color ColorGreyDefault = new Color(0.35f, 0.35f, 0.35f, 1);
        public static Color ColorGreyDark = new Color(0.15f, 0.15f, 0.2f, 1);

        private static AnywhenPlayerBase _currentRuntimeSongPlayer;


        private static AnywhenPlayerBase CurrentRuntimeSongPlayer
        {
            get
            {
                if (!_currentRuntimeSongPlayer)
                {
                    var player = new GameObject("AnywhenComposerPlayer")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _currentRuntimeSongPlayer = player.AddComponent<AnywhenComposerPlayer>();
                    _currentRuntimeSongPlayer.Load(CurrentSong);
                    _currentRuntimeSongPlayer.SetupTracks();
                    _currentRuntimeSongPlayer.LoadInstruments();
                }

                return _currentRuntimeSongPlayer;
            }
        }


        private AnysongSection _sectionCopy;
        AnysongPattern _patternCopy;


        public class AnySelection
        {
            private int _currentSectionIndex;
            public int CurrentSectionIndex => _currentSectionIndex;
            private int _currentStepIndex;
            public int CurrentStepIndex => _currentStepIndex;
            private int _currentPatternIndex;
            public int CurrentPatternIndex => _currentPatternIndex;
            private int _currentTrackIndex;
            public int CurrentTrackIndex => _currentTrackIndex;

            public AnysongTrack CurrentSongTrack;
            public AnysongSection CurrentSection;
            public AnysongSectionTrack CurrentSectionTrack;
            public AnysongPatternStep CurrentStep;
            public AnysongPattern CurrentPattern;
            public SerializedProperty CurrentSectionProperty;
            public SerializedProperty CurrentSongTrackProperty;
            public SerializedProperty CurrentSectionTrackProperty;
            public SerializedProperty CurrentStepProperty;
            public SerializedProperty CurrentPatternProperty;

            private AnysongObject _anysongObject;
            private SerializedObject _song;

            public AnySelection(AnysongObject currentSong)
            {
                _anysongObject = currentSong;
                _song = new SerializedObject(_anysongObject);
                Refresh();
            }

            public void SetSectionIndex(int newSectionIndex)
            {
                _currentSectionIndex = newSectionIndex;
                Refresh();
            }

            public void SetStepIndex(int newStepIndex)
            {
                _currentStepIndex = newStepIndex;
                Refresh();
            }

            public void SetPatternIndex(int newPatternIndex)
            {
                _currentPatternIndex = newPatternIndex;
                Refresh();
            }

            public void SetTrackIndex(int newTrackIndex)
            {
                _currentTrackIndex = newTrackIndex;
                Refresh();
            }


            void Refresh()
            {
                CurrentSongTrack = _anysongObject.Tracks[_currentTrackIndex];
                CurrentSection = _anysongObject.Sections[_currentSectionIndex];
                CurrentSectionTrack = CurrentSection.tracks[_currentTrackIndex];
                _currentPatternIndex = Mathf.Min(_currentPatternIndex, CurrentSectionTrack.patterns.Count - 1);
                CurrentPattern = CurrentSectionTrack.patterns[_currentPatternIndex];
                CurrentStep = CurrentPattern.steps[_currentStepIndex];


                var section = _song.FindProperty("Sections").GetArrayElementAtIndex(_currentSectionIndex);
                var track = section.FindPropertyRelative("tracks").GetArrayElementAtIndex(_currentTrackIndex);
                var pattern = track.FindPropertyRelative("patterns").GetArrayElementAtIndex(_currentPatternIndex);
                var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(_currentStepIndex);

                CurrentPatternProperty = pattern;
                CurrentStepProperty = step;
                CurrentSongTrackProperty = _song.FindProperty("Tracks").GetArrayElementAtIndex(_currentTrackIndex);

                CurrentSectionProperty = section;
                CurrentSectionTrackProperty = track;
            }
        }

        private static AnySelection _currentSelection;
        public static AnySelection CurrentSelection => _currentSelection;
        private static AnySelection _currentHover;

        private float _scroll;
        public static bool TrackEdit;

        public enum InspectorModes
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
            songObject.Reset();

            _currentRuntimeSongPlayer = composerPlayer;
            CurrentSong = songObject;
            _currentRuntimeSongPlayer.SetupTracks();

            _currentRuntimeSongPlayer.LoadInstruments();
            EditorPrefs.SetString("AnyLoadedSong", AssetDatabase.GetAssetPath(songObject));
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            AnywhenRuntime.Log("Loaded: " + AssetDatabase.GetAssetPath(songObject));

            // Set the default UXML template if not already set
            if (!window.uxmlAsset)
                window.uxmlAsset =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongEditorWindow.uxml");


            if (!window.styleAsset)
                window.styleAsset =
                    AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/PackageAnywhen/Editor/USS/AnywhenEditorStyles.uss");

            _currentSelection = new AnySelection(songObject);
            _currentHover = new AnySelection(songObject);


            window.Show(true);

            foreach (var track in songObject.Tracks)
            {
                if (track.trackEnvelope.IsUnset())
                {
                    track.trackEnvelope.Initialize();
                }

                if (track.pitchLFOSettings.IsUnset())
                {
                    track.pitchLFOSettings.Initialize();
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
                CurrentSong.Play(AnysongObject.SongPlayModes.Edit);

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
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                AnywhenRuntime.Metronome.OnNextBar -= OnBar;
                AnysongSectionsView.HilightSection(-1, _currentSelection.CurrentSectionIndex);
                AnysongPatternView.ResetTriggered();
                AnysongProgressionsView.ResetTriggered();
            }
        }

        private void OnDestroy()
        {
            CurrentRuntimeSongPlayer.Stop();
            DestroyImmediate(CurrentRuntimeSongPlayer.gameObject);
            _currentRuntimeSongPlayer = null;
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
            AnysongPatternView.Draw(_sequencesPanel);
            AnysongInspectorView.Draw(_inspectorPanel);

            HandleTransportLogic();
            HandleSectionsLogic();
            HandleTracksLogic();
            HandleSequencesLogic();
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
            if (_currentSelection.CurrentSectionIndex == CurrentRuntimeSongPlayer.CurrentSong.CurrentSectionIndex)
            {
                for (var i = 0; i < CurrentSong.Tracks.Count; i++)
                {
                    AnysongPatternView.HilightStepIndex(i,
                        _currentSelection.CurrentSection.tracks[i].GetSelectedPatternIndex() ==
                        _currentSelection.CurrentSection.tracks[i].GetPlayingPatternIndex());
                }
            }
        }

        static void OnBar()
        {
            if (CurrentSong != CurrentRuntimeSongPlayer.CurrentSong) return;
            AnysongSectionsView.HilightSection(CurrentRuntimeSongPlayer.CurrentSong.CurrentSectionIndex, _currentSelection.CurrentSectionIndex);

            var ints = CurrentRuntimeSongPlayer.EditorGetPlayingTrackPatternIndexes();
            for (var i = 0; i < ints.Length; i++)
            {
                var patternIndex = ints[i];
                AnysongProgressionsView.HilightPattern(i, patternIndex, _currentSelection.CurrentPatternIndex);
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
                        _currentSelection.SetPatternIndex(0);
                        _currentSelection.SetSectionIndex(Int32.Parse(btn.tooltip));


                        SetInspectorMode(InspectorModes.Sections);
                        AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
                        AnysongPatternView.Draw(_sequencesPanel);
                        AnysongPatternView.Refresh();
                        HandleSequencesLogic();
                        //HandlePatternsLogic();
                        RefreshSectionLockIndex();
                        HandleSectionsLogic();
                        HandleProgressionLogic();

                        AnysongTracksView.UpdateMuteSoleState();
                        AnysongSectionsView.HilightSection(CurrentRuntimeSongPlayer.CurrentSong.CurrentSectionIndex,
                            _currentSelection.CurrentSectionIndex);
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
                        SetSelectionFromTooltip(btn.tooltip);
                        SetInspectorMode(InspectorModes.Track);
                    }

                    TrackEdit = true;
                    AnysongPatternView.Draw(_sequencesPanel);
                    HandleSequencesLogic();
                });
            });

            _tracksPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreateNewTrack(); });
            _tracksPanel.Q<Button>("RemoveButton").RegisterCallback((ClickEvent ev) => { DeleteTrack(); });
        }


        void HandleProgressionLogic()
        {
            //_progressionPanel.Query<Button>("ProgressionButton").ForEach(button =>
            //{
            //    button.RegisterCallback((ClickEvent ev) =>
            //    {
            //        if (ev.currentTarget is Button btn)
            //        {
            //            SetSelectionFromTooltip(btn.tooltip);
            //            SetInspectorMode(InspectorModes.Progression);
            //        }
            //    });
            //});
            
            _progressionPanel.Query<Button>("AddButton").ForEach(button =>
            {
                button.RegisterCallback((ClickEvent ev) =>
                {
                    if (ev.currentTarget is Button btn)
                    {
                        CreatePattern(Int32.Parse(btn.tooltip));
                        AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
                        //AnysongProgressionsView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex,
                        //    _currentSelection.CurrentPatternIndex);
                        HandleProgressionLogic();
                        AnysongPatternView.Refresh();
                    }
                });
            });
            
        }


        void CreatePattern(int trackIndex)
        {
            Debug.Log("create new pattern");

            var newPattern = new AnysongPattern();
            newPattern.Init();
            var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

            thisTrack.patterns.Add(newPattern);
            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
            thisTrack.SetSelectedPattern(thisTrack.patterns.Count - 1);

            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
            HandleSequencesLogic();
            HandleProgressionLogic();
        }


        void DeletePattern(int trackIndex)
        {
            Debug.Log("remove pattern");

            var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

            thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);
            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
            thisTrack.SetSelectedPattern(thisTrack.patterns.Count - 1);
            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
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
            AnysongPatternView.Draw(_sequencesPanel);
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
            _currentSelection.SetTrackIndex(0);

            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongPatternView.Draw(_sequencesPanel);
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
            _currentSelection.SetSectionIndex(CurrentSong.Sections.Count - 1);

            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongPatternView.Draw(_sequencesPanel);
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
            AnysongPatternView.Refresh();
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }


        void RefreshSectionLockIndex()
        {
            if (!CurrentSong.SectionEditLock) return;

            CurrentSong.SetEditSection(_currentSelection.CurrentSectionIndex);
            Debug.Log("set locked section to  " + _currentSelection.CurrentSectionIndex);

            //AnysongPlayerBrain.SetSectionLock(CurrentSectionLockIndex);
        }



        void ToggleSectionLock()
        {
            CurrentSong.SetEditSectionLock(!CurrentSong.SectionEditLock);
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }


        void HandleSequencesLogic()
        {
        }


        private void OnPointerEnterEvent(PointerEnterEvent evt)
        {
            if (evt.currentTarget is not Button btn) return;
            Debug.Log("entered " + btn.tooltip);
            //SetSelectionFromTooltip(btn.tooltip, _currentHover);
            _scroll = _currentHover.CurrentStep.rootNote;
        }


        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            if (evt.currentTarget is not Button btn) return;
            Debug.Log("left " + btn.tooltip);
            _currentHover.CurrentStep = null;
            _currentHover.CurrentPattern = null;
            _currentHover.CurrentSectionTrack = null;
            _scroll = 0;
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.currentTarget is not Button btn) return;
            SetSelectionFromTooltip(btn.tooltip);
            SetInspectorMode(InspectorModes.Step);

            AnysongPatternView.Draw(_sequencesPanel);
            HandleSequencesLogic();
        }


        void SetSelectionFromTooltip(string tooltip, AnySelection targetSelection = null)
        {
            var str = tooltip.Split("-");
            targetSelection ??= _currentSelection;
            targetSelection.SetStepIndex(Int32.Parse(str[0]));
            targetSelection.SetTrackIndex(Int32.Parse(str[1]));
            targetSelection.SetPatternIndex(Int32.Parse(str[2]));
        }


        public static AnysongPattern GetCurrentPlayingPatternForTrack(int trackIndex)
        {
            return CurrentSong.Sections[CurrentRuntimeSongPlayer.CurrentSong.CurrentSectionIndex].tracks[trackIndex].GetPlayingPattern();
        }

        public static AnysongPattern GetPatternFromTooltip(string tooltip)
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

            return CurrentSong.Sections[_currentSelection.CurrentSectionIndex].tracks[trackIndex].patterns[patternIndex].steps[stepIndex];
        }

        public void SetInspectorMode(InspectorModes inspectorMode)
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
                        AnysongPatternView.Refresh);
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
                        () => { CurrentRuntimeSongPlayer.UpdateTrackInstrument(_currentSelection.CurrentSongTrack); });

                    break;
                case InspectorModes.Step:
                    AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty, null);
                    HandleStepUtilsLogic();

                    break;
                case InspectorModes.Progression:
                    AnysongInspectorView.DrawProgression();
                    _inspectorPanel.Q<Button>("AddButton")
                        .RegisterCallback((ClickEvent ev) => { CreatePattern(_currentSelection.CurrentTrackIndex); });
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inspectorMode), inspectorMode, null);
            }
        }


        void RandomizeMelody()
        {
            _currentSelection.CurrentPattern.RandomizeMelody();
            AnysongPatternView.Refresh();
        }

        void RandomizeRhythm()
        {
            _currentSelection.CurrentPattern.RandomizeRhythm();
            AnysongPatternView.Refresh();
        }

        void ScrubPattern(int direction)
        {
            _currentSelection.CurrentPattern.Scrub(direction);
            AnysongPatternView.Refresh();
        }


        void HandlePatternUtilsLogic()
        {
            _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) => { CopyPattern(_currentSelection.CurrentPattern); });
            _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) => { PastePattern(); });
        }

        void HandleStepUtilsLogic()
        {
            //_inspectorPanel.Q<Button>("CopyButton")
            //    .RegisterCallback<ClickEvent>((evt) => { CopyStep(_currentSelection.CurrentStep); });
            //_inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) =>
            //{
            //    PasteStep(_currentSelection.CurrentPattern, _currentSelection.CurrentStep);
            //});
        }


        public static AnysongEditorWindow Create(Object asset)
        {
            var window = CreateWindow<AnysongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
            return window;
        }


        void ClearPattern()
        {
            var newPattern = new AnysongPattern();
            newPattern.Init();
            //CurrentSectionTrack.EditorCurrentPattern = newPattern;
        }

        void CopyPattern(AnysongPattern pattern)
        {
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            _patternCopy = pattern.Clone();
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


        public static void SelectPatternStep(AnysongPatternStep patternStep, int stepIndex)
        {
            _currentSelection.SetStepIndex(stepIndex);
            AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty, null);
            AnysongPatternView.Refresh();
        }
    }
}