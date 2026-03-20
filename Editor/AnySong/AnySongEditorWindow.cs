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
                    _currentRuntimeSongPlayer.SetupTracks(CurrentSong.Tracks);
                    _currentRuntimeSongPlayer.LoadInstruments();
                }

                return _currentRuntimeSongPlayer;
            }
        }

        private AnysongSection _sectionCopy;
        static AnysongPattern _patternCopy;


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

            public AnysongTrackSettings CurrentSongTrackSettings;
            public AnysongSection CurrentSection;
            public AnysongSectionTrack CurrentSectionTrack;
            public AnysongPatternStep CurrentStep;
            public AnysongPattern CurrentPattern;
            public SerializedProperty CurrentSectionProperty;
            public SerializedProperty CurrentSongTrackProperty;
            public SerializedProperty CurrentSectionTrackProperty;
            public SerializedProperty CurrentStepProperty;
            public SerializedProperty CurrentPatternProperty;

            private readonly AnysongObject _anysongObject;
            private readonly SerializedObject _song;

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
                _song.Update();
                CurrentSongTrackSettings = _anysongObject.Tracks[_currentTrackIndex];
                CurrentSection = _anysongObject.Sections[_currentSectionIndex];
                CurrentSectionTrack = CurrentSection.tracks[_currentTrackIndex];
                _currentPatternIndex = Mathf.Clamp(_currentPatternIndex, 0, CurrentSectionTrack.patterns.Count - 1);
                CurrentPattern = CurrentSectionTrack.patterns[_currentPatternIndex];
                _currentStepIndex = Mathf.Clamp(_currentStepIndex, 0, CurrentPattern.steps.Count - 1);
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
            window.minSize = new Vector2(1450, 850);
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
            //_currentRuntimeSongPlayer.SetupTracks(CurrentSong.Tracks);
            _currentRuntimeSongPlayer.LoadInstruments();

            EditorPrefs.SetString("AnyLoadedSong", AssetDatabase.GetAssetPath(songObject));
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            AnywhenRuntime.Log("Loaded: " + AssetDatabase.GetAssetPath(songObject));

            // Set the default UXML template if not already set
            if (!window.uxmlAsset)
                window.uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PackageAnywhen/Editor/uxml/AnysongEditorWindow.uxml");


            if (!window.styleAsset)
                window.styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/PackageAnywhen/Editor/USS/AnywhenEditorStyles.uss");

            _currentSelection = new AnySelection(songObject);


            window.Show(true);

            foreach (var track in songObject.Tracks)
            {
                if (track.trackEnvelope.IsUnset())
                {
                    track.trackEnvelope.Initialize();
                }

                if (track.trackLFO.IsUnset())
                {
                    track.trackLFO.Initialize();
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
                _currentRuntimeSongPlayer.Load(CurrentSong);

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
            if (_currentRuntimeSongPlayer)
            {
                CurrentRuntimeSongPlayer.Stop();
                DestroyImmediate(CurrentRuntimeSongPlayer.gameObject);
                _currentRuntimeSongPlayer = null;
            }

            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            AnywhenRuntime.Metronome.OnNextBar -= OnBar;

            CurrentSong = null;
            _currentSelection = null;

            AnysongPatternView.Clear();
            AnysongProgressionsView.Clear();
            AnysongSectionsView.Clear();
            AnysongTracksView.Clear();
            AnysongTransportView.Clear();
            AnysongInspectorView.Clear();
        }

        static void SetTestIntensity(float value)
        {
            if (!CurrentRuntimeSongPlayer) return;
            CurrentRuntimeSongPlayer.SetIntensity(value);
        }


        private VisualElement _mainViewPanel;
        private VisualElement _transportPanel;
        private static VisualElement _sequencesPanel;
        private VisualElement _sectionsPanel;
        private VisualElement _tracksPanel;
        private static VisualElement _inspectorPanel;
        private static VisualElement _progressionPanel;
        [SerializeField] VisualTreeAsset uxmlAsset;
        [SerializeField] StyleSheet styleAsset;

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            if (!CurrentSong)
            {
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
            HandleProgressionLogic();
        }

        void HandleTransportLogic()
        {
            _transportPanel.Q<Button>("PlayButton").RegisterCallback<ClickEvent>((evt) =>
            {
                ToggleIsPlaying();
                AnysongTransportView.RefreshPlaybuttonState(_isPLaying);
            });

            _transportPanel.Q<Slider>("TestIntensitySlider")
                .RegisterValueChangedCallback(evt => { SetTestIntensity(evt.newValue); });
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
            AnysongSectionsView.HilightSection(CurrentRuntimeSongPlayer.CurrentSong.CurrentSectionIndex,
                _currentSelection.CurrentSectionIndex);

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
                        AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
                        AnysongPatternView.Draw(_sequencesPanel);
                        AnysongPatternView.Refresh();
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
                        SetSelectionFromTooltip(btn.tooltip, _currentSelection);
                        SetInspectorMode(InspectorModes.Track);

                        TrackEdit = true;
                        AnysongPatternView.Draw(_sequencesPanel);
                        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
                        HandleTracksLogic();
                    }
                });
            });

            _tracksPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreateNewTrack(); });
            _tracksPanel.Q<Button>("RemoveButton").RegisterCallback((ClickEvent ev) => { DeleteTrack(); });
        }


        static void HandleProgressionLogic()
        {
            _progressionPanel.Query<Button>("AddButton").ForEach(button =>
            {
                button.RegisterCallback((ClickEvent ev) =>
                {
                    if (ev.currentTarget is Button btn)
                    {
                        CreatePattern(Int32.Parse(btn.tooltip));
                        AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);

                        HandleProgressionLogic();
                        AnysongPatternView.Refresh();
                    }
                });
            });
        }


        static void CreatePattern(int trackIndex)
        {
            Debug.Log("create new pattern");

            var newPattern = new AnysongPattern();
            newPattern.Init();
            var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

            thisTrack.patterns.Add(newPattern);
            _currentSelection.SetStepIndex(0);
            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
            _currentSelection.SetTrackIndex(trackIndex);
            thisTrack.SetSelectedPattern(_currentSelection.CurrentPatternIndex);

            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
            HandleProgressionLogic();
        }


        static void DeletePattern()
        {
            Debug.Log("remove pattern");
            var thisTrack = _currentSelection.CurrentSectionTrack;
            thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);
            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
            thisTrack.SetSelectedPattern(thisTrack.patterns.Count - 1);
            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
            AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
            HandleProgressionLogic();
        }

        void CreateNewTrack()
        {
            var newTrack = new AnysongTrackSettings();
            newTrack.Init();
            CurrentSong.Tracks.Add(newTrack);
            foreach (var section in CurrentSong.Sections)
            {
                section.AddSongTrack(newTrack);
            }

            _currentSelection.SetTrackIndex(CurrentSong.Tracks.Count - 1);
            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongPatternView.Draw(_sequencesPanel);
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
            _currentSelection.SetTrackIndex(CurrentSong.Tracks.Count - 1);


            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongPatternView.Draw(_sequencesPanel);
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
        }


        void ToggleSectionLock()
        {
            CurrentSong.SetEditSectionLock(!CurrentSong.SectionEditLock);
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }


        void SetSelectionFromTooltip(string tooltip, AnySelection targetSelection = null)
        {
            var str = tooltip.Split("-");
            targetSelection ??= _currentSelection;
            targetSelection.SetStepIndex(Int32.Parse(str[0]));
            targetSelection.SetTrackIndex(Int32.Parse(str[1]));
            targetSelection.SetPatternIndex(Int32.Parse(str[2]));
        }


        public void SetInspectorMode(InspectorModes inspectorMode)
        {
            switch (inspectorMode)
            {
                case InspectorModes.Sections:
                    AnysongInspectorView.DrawSection(_currentSelection.CurrentSectionProperty);

                    _inspectorPanel.Q<Button>("CopyButton").RegisterCallback((ClickEvent ev) => { CopySection(); });
                    _inspectorPanel.Q<Button>("PasteButton").RegisterCallback((ClickEvent ev) => { PasteSection(); });
                    break;
                case InspectorModes.Pattern:
                    AnysongInspectorView.DrawPattern(_currentSelection.CurrentPatternProperty, AnysongPatternView.Refresh);
                    _inspectorPanel.Q<Button>("DeleteButton").RegisterCallback((ClickEvent ev) => { DeletePattern(); });
                    _inspectorPanel.Q<Button>("ScrubForward").RegisterCallback((ClickEvent ev) => { ScrubPattern(-1); });
                    _inspectorPanel.Q<Button>("ScrubBack").RegisterCallback((ClickEvent ev) => { ScrubPattern(1); });

                    _inspectorPanel.Q<Button>("RandomizeMelody").RegisterCallback((ClickEvent ev) => { RandomizeMelody(); });
                    _inspectorPanel.Q<Button>("RandomizeRhythm").RegisterCallback((ClickEvent ev) => { RandomizeRhythm(); });


                    break;
                case InspectorModes.Track:
                    AnysongInspectorView.DrawTrack(_currentSelection,
                        () => { CurrentRuntimeSongPlayer.UpdateTrackInstrument(_currentSelection.CurrentSongTrackSettings); });

                    break;
                case InspectorModes.Step:
                    AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty, null);
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

        public static void HandleInspectorLogic()
        {
            _inspectorPanel.Q<Button>("DeleteButton").RegisterCallback((ClickEvent ev) => { DeletePattern(); });

            _inspectorPanel.Q<Button>("ClearButton").RegisterCallback((ClickEvent ev) => { ClearPattern(); });

            _inspectorPanel.Q<Button>("ScrubForward").RegisterCallback((ClickEvent ev) => { ScrubPattern(-1); });
            _inspectorPanel.Q<Button>("ScrubBack").RegisterCallback((ClickEvent ev) => { ScrubPattern(1); });

            _inspectorPanel.Q<Button>("RandomizeMelody").RegisterCallback((ClickEvent ev) => { RandomizeMelody(); });
            _inspectorPanel.Q<Button>("RandomizeRhythm").RegisterCallback((ClickEvent ev) => { RandomizeRhythm(); });
            _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) => { CopyPattern(_currentSelection.CurrentPattern); });
            _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) => { PastePattern(); });
        }


        static void RandomizeMelody()
        {
            _currentSelection.CurrentPattern.RandomizeMelody();
            AnysongPatternView.Refresh();
        }

        static void RandomizeRhythm()
        {
            _currentSelection.CurrentPattern.RandomizeRhythm();
            AnysongPatternView.Refresh();
        }

        static void ScrubPattern(int direction)
        {
            _currentSelection.CurrentPattern.Scrub(direction);
            AnysongPatternView.Refresh();
        }


        public static AnysongEditorWindow Create(Object asset)
        {
            var window = CreateWindow<AnysongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
            return window;
        }


        static void ClearPattern()
        {
            Debug.Log("ClearPattern");
            CurrentSelection.CurrentPattern.Clear();
            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
        }

        static void CopyPattern(AnysongPattern pattern)
        {
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            _patternCopy = pattern.Clone();
        }

        static void PastePattern()
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
            AnysongPatternView.Refresh();
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