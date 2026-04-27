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

        static AnywhenAudioGenrator _currentPlayer;
        static AnywhenAudioMetronome _currentMetronome;
        //private static AnywhenPlayerBase _currentRuntimeSongPlayer;

        //private static AnywhenPlayerBase CurrentRuntimeSongPlayer
        //{
        //    get
        //    {
        //        if (!_currentRuntimeSongPlayer)
        //        {
        //        }
//
        //        return _currentRuntimeSongPlayer;
        //    }
        //}

        private GameObject _playerObject;

        void CreatePlayer()
        {
            Debug.Log("Create player");
            _playerObject = new GameObject("AnywhenComposerPlayer")
            {
                // hideFlags = HideFlags.HideAndDontSave
            };
            var metronomeSource = _playerObject.AddComponent<AudioSource>();
            _currentMetronome = CreateInstance<AnywhenAudioMetronome>();
            metronomeSource.generator = _currentMetronome;
            metronomeSource.Play();

            var songSource = _playerObject.AddComponent<AudioSource>();
            _currentPlayer = CreateInstance<AnywhenAudioGenrator>();
            _currentPlayer.SetSong(CurrentSong);
            songSource.generator = _currentPlayer;
            songSource.Play();
        }

        private AnysongSection _sectionCopy;
        static AnysongPattern _patternCopy;

        private VisualElement _mainViewPanel;
        private VisualElement _transportPanel;
        private static VisualElement _sequencesPanel;
        private VisualElement _sectionsPanel;
        private VisualElement _tracksPanel;
        private static VisualElement _inspectorPanel;
        private static VisualElement _progressionPanel;
        [SerializeField] VisualTreeAsset uxmlAsset;
        [SerializeField] StyleSheet styleAsset;

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
        }

        public static void LoadSong(AnysongObject songObject)
        {
            CurrentSong = songObject;
            _currentSelection = new AnySelection(songObject);
            //_currentRuntimeSongPlayer.SetupTracks(CurrentSong.Tracks);
            //  _currentRuntimeSongPlayer.LoadInstruments();

            EditorPrefs.SetString("AnyLoadedSong", AssetDatabase.GetAssetPath(songObject));
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            AnywhenRuntime.Log("Loaded: " + AssetDatabase.GetAssetPath(songObject));

            // Set the default UXML template if not already set
            string basePath = "Assets/PackageAnywhen/Editor/";
            if (!window.uxmlAsset)
                window.uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(basePath + "uxml/AnysongEditorWindow.uxml");

            if (!window.styleAsset)
                window.styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(basePath + "USS/AnywhenEditorStyles.uss");

            _currentSelection = new AnySelection(songObject);


            window.Show(true);

            foreach (var track in songObject.Tracks)
            {
                if (track.trackAudioEnvelope1.IsUnset())
                {
                    track.trackAudioEnvelope1.Initialize();
                }

                if (track.trackAudioLFO1.IsUnset())
                {
                    track.trackAudioLFO1.Initialize();
                }
            }
        }

        private static bool _isPLaying;

        private static void ToggleIsPlaying()
        {
            _isPLaying = !_isPLaying;

            if (_isPLaying)
            {
                CurrentSong.Rebuild();
                _currentPlayer.Load(CurrentSong);
                //AnywhenRuntime.SetPreviewMode(true, CurrentRuntimeSongPlayer);
                AnysongSectionsView.RefreshSectionLocked();
                AnywhenMetronome.Instance.SetTempo(CurrentSong.tempo);
                AnywhenAudioMetronome.OnAudioTick += OnTick16;
                AnywhenAudioMetronome.OnBar += OnBar;
                OnBar();
            }
            else
            {
                //AnywhenRuntime.SetPreviewMode(false, CurrentRuntimeSongPlayer);
                AnywhenAudioMetronome.OnAudioTick -= OnTick16;
                AnywhenAudioMetronome.OnBar -= OnBar;
                AnysongSectionsView.SetPlayingSectionIndex(-1);
                AnysongPatternView.ResetTriggered();
                AnysongProgressionsView.ResetTriggered();
            }
        }

        private void OnDestroy()
        {
            _currentPlayer = null;
            if (_playerObject)
            {
                DestroyImmediate(_playerObject);
                _playerObject = null;
            }

            //if (_currentRuntimeSongPlayer)
            //{
            //    CurrentRuntimeSongPlayer.Stop();
            //    DestroyImmediate(CurrentRuntimeSongPlayer.gameObject);
            //    _currentRuntimeSongPlayer = null;
            //}

            //AnywhenRuntime.Metronome.a -= OnTick16;
            //AnywhenRuntime.Metronome.OnNextBar -= OnBar;

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
            Debug.LogWarning("set intensity not implemented yet");
            //if (!CurrentRuntimeSongPlayer) return;
            //CurrentRuntimeSongPlayer.SetIntensity(value);
        }


        public void CreateGUI()
        {
            CreatePlayer();
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

        private static void OnTick16(MetronomeTickEvent tick)
        {
            if (_currentSelection == null) return;
            //bool doHighLight = _currentSelection.CurrentSectionIndex == CurrentRuntimeSongPlayer.CurrentSectionIndex &&
            //                   _currentSelection.CurrentPatternIndex ==
            //                   CurrentRuntimeSongPlayer.GetPlayingPatternIndexForTrackIndex(_currentSelection.CurrentTrackIndex);

            AnysongPatternView.HilightStepIndex(_currentSelection.CurrentTrackIndex, true);
        }

        static void OnBar()
        {
            Debug.LogWarning("OnBar not implemented yet");
            //AnysongSectionsView.SetPlayingSectionIndex(CurrentRuntimeSongPlayer.GetPlayingSectionIndex());
//
            //if (CurrentSelection.CurrentSectionIndex == CurrentRuntimeSongPlayer.CurrentSectionIndex)
            //{
            //    for (var i = 0; i < CurrentSong.Tracks.Count; i++)
            //    {
            //        AnysongProgressionsView.SetIsPatternPlaying(i,
            //            CurrentRuntimeSongPlayer.GetPlayingPatternIndexForTrackIndex(i));
            //    }
            //}
            //else
            //{
            //    for (var i = 0; i < CurrentSong.Tracks.Count; i++)
            //    {
            //        AnysongProgressionsView.SetIsPatternPlaying(i, -1);
            //    }
            //}
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
                        HandleSectionsLogic();
                        HandleProgressionLogic();

                        AnysongTracksView.UpdateMuteSoleState();
                        Debug.LogWarning("AnysongSectionsView not completely implemented");
                        //AnysongSectionsView.SetPlayingSectionIndex(CurrentRuntimeSongPlayer.CurrentSectionIndex);
                        AnysongProgressionsView.Refresh();
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
                        AnysongPatternView.Draw(_sequencesPanel);
                        AnysongTracksView.Draw(_tracksPanel, CurrentSong);
                        AnysongProgressionsView.Refresh();
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
            Debug.LogWarning("create new pattern not implemented yet");

            var newPattern = new AnysongPattern();
            newPattern.Init();
            var thisTrack = _currentSelection.CurrentSection.tracks[trackIndex];

            //thisTrack.patterns.Add(newPattern);
            _currentSelection.SetStepIndex(0);
            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
            _currentSelection.SetTrackIndex(trackIndex);

            AnysongPatternView.Draw(_sequencesPanel);
            AnysongPatternView.Refresh();
            HandleProgressionLogic();
        }


        static void DeletePattern()
        {
            Debug.LogWarning("remove pattern not implemented yet");
            var thisTrack = _currentSelection.CurrentSectionTrack;
            //thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);

            _currentSelection.SetPatternIndex(thisTrack.patterns.Count - 1);
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
            _sectionCopy = _currentSelection.CurrentSection.Clone();
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }

        void PasteSection()
        {
            Debug.Log("PasteSection");
            CurrentSong.Sections[_currentSelection.CurrentSectionIndex] = _sectionCopy;
            AnysongPatternView.Refresh();
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }


        void ToggleSectionLock()
        {
            _currentPlayer.SetSectionLock(!_currentPlayer.SectionLockState, _currentSelection.CurrentSectionIndex);
            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            HandleSectionsLogic();
        }

        public static bool IsSectionLocked => _currentPlayer.SectionLockState;

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
                    AnysongInspectorView.DrawTrack(() =>
                    {
                        Debug.Log("AnysongTracksView not completely implemented");
                        //CurrentRuntimeSongPlayer.UpdateTrackInstrument(_currentSelection.CurrentSongTrackSettings);
                    });

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
            _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) =>
            {
                CopyPattern(_currentSelection.CurrentPattern);
            });
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
            for (var i = 0; i < _currentSelection.CurrentSectionTrack.patterns.Count; i++)
            {
                if (Equals(_currentSelection.CurrentSectionTrack.patterns[i], _currentSelection.CurrentPattern))
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
                newAddButton.AddToClassList("section-add-button");
                buttons.Add(newAddButton);
            }

            if (removeButton)
            {
                var deleteButton = new Button
                {
                    name = "RemoveButton",
                    text = "-"
                };
                deleteButton.AddToClassList("section-add-button");
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