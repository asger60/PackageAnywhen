using System;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Editor.AnySong
{
    public class AnysongEditorWindow : EditorWindow
    {
        public static AnysongObject CurrentSong { get; private set; }

        private bool _currentPatternIsBase;

        private Event _currentEvent;

        public static Color ColorHilight1 = new Color(0.8f, 0.9f, 0, 1);
        public static Color ColorHilight2 = new Color(0.8f, 0.4f, 0, 1);
        public static Color ColorHilight3 = new Color(0.2f, 0.4f, 0.8f, 1);
        public static Color ColorGreyDefault = new Color(0.35f, 0.35f, 0.35f, 1);
        public static Color ColorGreyDark = new Color(0.15f, 0.15f, 0.2f, 1);
        public static Color ColorGreyAccent = new Color(0.35f, 0.3f, 0.3f, 1);

        private static AnysongPlayer _currentRuntimeSongPlayer;
        private AnyPatternStep _stepCopy;

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
            public AnyPatternStep CurrentStep;
            public AnyPattern CurrentPattern;
            public SerializedProperty CurrentSectionProperty;
            public SerializedProperty CurrentSongTrackProperty;
            public SerializedProperty CurrentSectionTrackProperty;
            public SerializedProperty CurrentStepProperty;
            public SerializedProperty CurrentPatternProperty;
        }

        private static AnySelection _currentSelection = new AnySelection();
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


        [MenuItem("Anywhen/Anysong Editor")]
        public static void ShowModuleWindow()
        {
            Object[] selection = Selection.GetFiltered(typeof(AnysongObject), SelectionMode.Assets);
            if (selection.Length > 0)
            {
                AnysongObject songObject = selection[0] as AnysongObject;
                if (songObject != null)
                {
                    AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));

                    CurrentSong = songObject;
                    window.Show(true);
                    EditorPrefs.SetString("AnyLoadedSong", AssetDatabase.GetAssetPath(songObject));
                    Debug.Log("Loaded: " + AssetDatabase.GetAssetPath(songObject));
                    window.titleContent = new GUIContent("Anysong window");
                    window.minSize = new Vector2(450, 200);
                    window.maxSize = new Vector2(1920, 720);
                    window.CreateGUI();
                }
            }
        }

        private VisualElement _mainViewPanel;
        private VisualElement _sequencesPanel;
        private VisualElement _sectionsPanel;
        private VisualElement _tracksPanel;
        private VisualElement _inspectorPanel;
        private VisualElement _progressionPanel;


        public void CreateGUI()
        {
            _currentSelection = new AnySelection
            {
                CurrentPatternIndex = 0,
                CurrentStepIndex = 0,
            };


            rootVisualElement.Clear();
            if (CurrentSong == null)
            {
                CurrentSong = Selection.activeObject as AnysongObject;
                if (CurrentSong == null)
                {
                    CurrentSong = AssetDatabase.LoadAssetAtPath<AnysongObject>(EditorPrefs.GetString("AnyLoadedSong"));
                }
            }


            if (CurrentSong.tempo == 0)
            {
                Debug.Log("updating song tempo");
                CurrentSong.tempo = 100;
            }

            if (CurrentSong.Sections.Count == 0)
            {
                CreateNewSection();
            }


            if (CurrentSong.Tracks.Count == 0)
            {
                CreateNewTrack();
            }

            _sectionsPanel = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 60,
                }
            };

            rootVisualElement.Add(_sectionsPanel);

            VisualElement columnsPanel = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            rootVisualElement.Add(columnsPanel);

            var mainContent = new VisualElement()
            {
                style =
                {
                    flexBasis = new StyleLength(Length.Percent(100)),
                    flexDirection = FlexDirection.Row,
                }
            };

            _mainViewPanel = new ScrollView(ScrollViewMode.Vertical);


            _tracksPanel = new VisualElement()
            {
                style = { width = new StyleLength(300), }
            };


            _progressionPanel = new VisualElement()
            {
                style = { width = 80, }
            };


            _inspectorPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { width = 500, }
            };

            _sequencesPanel = new VisualElement()
            {
                style = { flexBasis = new StyleLength(Length.Percent(100)) }
            };


            mainContent.Add(_tracksPanel);
            mainContent.Add(_progressionPanel);
            mainContent.Add(_sequencesPanel);
            _mainViewPanel.Add(mainContent);

            columnsPanel.Add(_mainViewPanel);
            columnsPanel.Add(_inspectorPanel);


            AnysongSectionsView.Draw(_sectionsPanel, CurrentSong, _currentSelection.CurrentSectionIndex);
            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongProgressionsView.Draw(_progressionPanel, CurrentSong);
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0], _currentSelection.CurrentSectionIndex);
            AnysongInspectorView.Draw(_inspectorPanel);

            HandleSectionsLogic();
            HandleTracksLogic();
            HandleProgressionLogic();
            HandleSequencesLogic();
            HandlePatternsLogic();
            RegisterKeyboardInputs();
            RegisterScrollWheelInputs();
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }


        private void EditorApplicationOnplayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                foreach (var sPlayer in FindObjectsOfType<AnysongPlayer>())
                {
                    if (sPlayer.AnysongObject == CurrentSong)
                    {
                        _currentRuntimeSongPlayer = sPlayer;
                        AnywhenRuntime.Metronome.OnTick16 += OnTick16;
                        AnywhenRuntime.Metronome.OnNextBar += OnBar;
                    }
                }
            }
            else
            {
                CurrentSong.Reset();
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
                AnywhenRuntime.Metronome.OnNextBar -= OnBar;
                _currentRuntimeSongPlayer = null;
                AnysongSectionsView.HilightSection(-1, _currentSelection.CurrentSectionIndex);
            }
        }

        private void OnTick16()
        {
            if (_currentSelection.CurrentSectionIndex == _currentRuntimeSongPlayer.CurrentSectionIndex)
            {
                //int index = (int)Mathf.Repeat(AnywhenRuntime.Metronome.Sub16, 16);
                for (var i = 0; i < CurrentSong.Tracks.Count; i++)
                {
                    AnysongSequencesView.HilightStepIndex(i);
                }
            }
        }

        void OnBar()
        {
            //AnysongSequencesView.HilightStepIndex(-1);
            AnysongSectionsView.HilightSection(_currentRuntimeSongPlayer.CurrentSectionIndex, _currentSelection.CurrentSectionIndex);
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
            AnysongSequencesView.RefreshPatterns();
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
                AnysongSequencesView.RefreshPatterns();
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                _currentHover.CurrentStep.rootNote--;
                AnysongSequencesView.RefreshPatterns();
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
                        AnysongSequencesView.Draw(_sequencesPanel, _currentSelection.CurrentSection, _currentSelection.CurrentSectionIndex);
                        AnysongSequencesView.RefreshPatterns();
                        HandleSequencesLogic();
                        HandlePatternsLogic();
                        RefreshSectionLockIndex();
                        HandleSectionsLogic();
                        AnysongTracksView.UpdateMuteSoleState();
                        AnysongSectionsView.HilightSection(_currentRuntimeSongPlayer.CurrentSectionIndex, _currentSelection.CurrentSectionIndex);
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
        }

        void CreatePattern()
        {
            Debug.Log("create new pattern");

            var newPattern = new AnyPattern();
            newPattern.Init();
            _currentSelection.CurrentSectionTrack.patterns.Add(newPattern);
            _currentSelection.CurrentPatternIndex = _currentSelection.CurrentSectionTrack.patterns.Count - 1;

            AnysongSequencesView.Draw(_sequencesPanel, _currentSelection.CurrentSection, _currentSelection.CurrentSectionIndex);
            AnysongSequencesView.RefreshPatterns();
            HandleSequencesLogic();
            HandlePatternsLogic();
        }


        void DeletePattern()
        {
            Debug.Log("remove pattern");

            var thisTrack = CurrentSong.Sections[0].tracks[_currentSelection.CurrentTrackIndex];
            thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);
            _currentSelection.CurrentPatternIndex -= 1;
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[_currentSelection.CurrentSectionIndex],
                _currentSelection.CurrentSectionIndex);

            AnysongSequencesView.RefreshPatterns();
            HandleSequencesLogic();
            HandlePatternsLogic();
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
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0], _currentSelection.CurrentSectionIndex);
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
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0], _currentSelection.CurrentSectionIndex);
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
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[_currentSelection.CurrentSectionIndex],
                _currentSelection.CurrentSectionIndex);
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

            AnysongSequencesView.RefreshPatterns();


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

        void HandlePatternsLogic()
        {
            _sequencesPanel.Query<Button>("PatternButton").ForEach((button) =>
            {
                button.RegisterCallback<ClickEvent>((evt) =>
                {
                    if (evt.currentTarget is not Button btn) return;
                    _currentSelection = GetSelectionFromTooltip(btn.tooltip);

                    AnysongSequencesView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex, _currentSelection.CurrentPatternIndex);
                    AnysongSequencesView.RefreshPatterns();

                    _currentPatternIsBase = _currentSelection.CurrentPatternIndex == 0;
                    SetInspectorMode(InspectorModes.Pattern);
                });
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
            _currentSelection.CurrentSectionTrack = _currentSelection.CurrentSection.tracks[_currentSelection.CurrentTrackIndex];
            _currentSelection.CurrentPattern = _currentSelection.CurrentSectionTrack.GetPattern(_currentSelection.CurrentPatternIndex);
            _currentSelection.CurrentStep = _currentSelection.CurrentPattern.steps[_currentSelection.CurrentStepIndex];
            _currentSelection.CurrentSongTrack = CurrentSong.Tracks[_currentSelection.CurrentTrackIndex];


            var song = new SerializedObject(CurrentSong);
            var section = song.FindProperty("Sections").GetArrayElementAtIndex(_currentSelection.CurrentSectionIndex);
            var track = section.FindPropertyRelative("tracks").GetArrayElementAtIndex(_currentSelection.CurrentTrackIndex);
            var pattern = track.FindPropertyRelative("patterns").GetArrayElementAtIndex(_currentSelection.CurrentPatternIndex);
            var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(_currentSelection.CurrentStepIndex);

            _currentSelection.CurrentSectionProperty = section;
            _currentSelection.CurrentPatternProperty = pattern;
            _currentSelection.CurrentStepProperty = step;
            _currentSelection.CurrentSongTrackProperty = song.FindProperty("Tracks").GetArrayElementAtIndex(_currentSelection.CurrentTrackIndex);
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
            selection.CurrentSongTrackProperty = song.FindProperty("Tracks").GetArrayElementAtIndex(selection.CurrentTrackIndex);

            selection.CurrentSectionProperty = section;
            selection.CurrentSectionTrackProperty = track;

            return selection;
        }


        public static AnyPattern GetCurrentPlayingPatternForTrack(int trackIndex)
        {
            return CurrentSong.Sections[_currentRuntimeSongPlayer.CurrentSectionIndex].tracks[trackIndex]
                .GetPlayingPattern();
        }

        public static AnyPattern GetPatternFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            int stepIndex = Int32.Parse(str[0]);
            int trackIndex = Int32.Parse(str[1]);
            int patternIndex = Int32.Parse(str[2]);

            return CurrentSong.Sections[_currentSelection.CurrentSectionIndex].tracks[trackIndex].patterns[patternIndex];
        }

        public static AnyPatternStep GetPatternStepFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            int stepIndex = Int32.Parse(str[0]);
            int trackIndex = Int32.Parse(str[1]);
            int patternIndex = Int32.Parse(str[2]);

            return CurrentSong.Sections[_currentSelection.CurrentSectionIndex].tracks[trackIndex].patterns[patternIndex].steps[stepIndex];
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
                        AnysongSequencesView.RefreshPatterns);
                    _inspectorPanel.Q<Button>("DeleteButton").RegisterCallback((ClickEvent ev) => { DeletePattern(); });
                    _inspectorPanel.Q<Button>("ScrubForward").RegisterCallback((ClickEvent ev) => { ScrubPattern(-1); });
                    _inspectorPanel.Q<Button>("ScrubBack").RegisterCallback((ClickEvent ev) => { ScrubPattern(1); });


                    HandlePatternUtilsLogic();
                    break;
                case InspectorModes.Track:
                    AnysongInspectorView.DrawTrack(_currentSelection);

                    break;
                case InspectorModes.Step:
                    AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty, AnysongSequencesView.RefreshPatterns);
                    HandleStepUtilsLogic();
                    break;
                case InspectorModes.Progression:
                    AnysongInspectorView.DrawProgression(_currentSelection);
                    _inspectorPanel.Q<Button>("AddButton").RegisterCallback((ClickEvent ev) => { CreatePattern(); });
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inspectorMode), inspectorMode, null);
            }
        }

        void ScrubPattern(int direction)
        {
            _currentSelection.CurrentPattern.Scrub(direction);
            AnysongSequencesView.RefreshPatterns();
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


        void CopyStep(AnyPatternStep step)
        {
            Debug.Log("Copy step");
            _stepCopy = step.Clone();
        }

        void PasteStep(AnyPattern pattern, AnyPatternStep step)
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

            AnysongSequencesView.RefreshPatterns();
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
                var newSddButton = new Button
                {
                    name = "AddButton",
                    text = "+"
                };
                buttons.Add(newSddButton);
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
}