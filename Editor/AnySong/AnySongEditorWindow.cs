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
        public static Color ColorGreyDark = new Color(0.2f, 0.2f, 0.2f, 1);
        public static Color ColorGreyAccent = new Color(0.35f, 0.3f, 0.3f, 1);

        private AnysongPlayer _currentRuntimeSongPlayer;

        private AnyPatternStep _stepCopy;


        struct AnySelection
        {
            public int CurrentStepIndex;
            public int CurrentPatternIndex;
            public int CurrentTrackIndex;


            public AnysongTrack CurrentSongTrack;

            public AnysongSectionTrack CurrentSectionTrack;
            public AnyPatternStep CurrentStep;
            public AnyPattern CurrentPattern;


            public SerializedProperty CurrentSongTrackProperty;
            public SerializedProperty CurrentStepProperty;
            public SerializedProperty CurrentPatternProperty;
        }

        private AnySelection _currentSelection = new AnySelection();
        private AnySelection _currentHover;

        private float _scroll;

        public enum InspectorModes
        {
            Sections,
            Pattern,
            Track,
            Step
        }


        private InspectorModes _currentInspectorMode = InspectorModes.Sections;

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

        private VisualElement _sequencesPanel;
        private VisualElement _tracksPanel;
        private VisualElement _inspectorPanel;

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


            VisualElement threeColumnsPanel = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            rootVisualElement.Add(threeColumnsPanel);

            _tracksPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { width = 300, }
            };

            _inspectorPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { width = 500, }
            };

            _sequencesPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { flexBasis = new StyleLength(Length.Percent(100)) }
            };

            threeColumnsPanel.Add(_tracksPanel);
            threeColumnsPanel.Add(_sequencesPanel);
            threeColumnsPanel.Add(_inspectorPanel);


            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);
            AnysongInspectorView.Draw(_inspectorPanel);

            HandleTracksLogic();
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
                _currentRuntimeSongPlayer = FindObjectOfType<AnysongPlayer>();
                AnywhenRuntime.Metronome.OnTick16 += OnTick16;
            }
            else
            {
                AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
            }
        }

        private void OnTick16()
        {
            for (var i = 0; i < CurrentSong.Sections[0].tracks.Count; i++)
            {
                AnysongSequencesView.HilightStepIndex(i, _currentRuntimeSongPlayer.GetStepForTrack(0));
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
            AnysongSequencesView.RefreshPatterns();
        }


        private void OnKeyDown(KeyDownEvent evt)
        {
            //Debug.Log("pressed " + evt.keyCode);
            if (_currentHover.CurrentStep == null) return;
            if (evt.keyCode == KeyCode.C)
            {
                if (_currentSelection.CurrentStep != null)
                {
                    CopyStep(_currentSelection.CurrentStep);
                }
            }

            if (evt.keyCode == KeyCode.V)
            {
                if (_currentSelection.CurrentStep != null && _stepCopy != null)
                {
                    PasteStep();
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

            _tracksPanel.Q<Button>("AddTrackButton").RegisterCallback((ClickEvent ev) => { CreateNewTrack(); });
            _tracksPanel.Q<Button>("RemoveTrackButton").RegisterCallback((ClickEvent ev) => { RemoveTrack(); });
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
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);
            HandleSequencesLogic();
            HandleTracksLogic();
        }

        void RemoveTrack()
        {
            foreach (var section in CurrentSong.Sections)
            {
                section.RemoveSongTrack(_currentSelection.CurrentTrackIndex);
            }

            CurrentSong.Tracks.RemoveAt(_currentSelection.CurrentTrackIndex);
            _currentSelection.CurrentTrackIndex = 0;
            
            AnysongTracksView.Draw(_tracksPanel, CurrentSong);
            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);
            HandleSequencesLogic();
            HandleTracksLogic();
        }

        void CreateNewSection()
        {
            var newSection = new AnysongSection();
            newSection.Init(CurrentSong.Tracks);
            CurrentSong.Sections.Add(newSection);
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

                    AnysongSequencesView.SetPatternIndexForTrack(_currentSelection.CurrentTrackIndex,
                        _currentSelection.CurrentPatternIndex);
                    AnysongSequencesView.RefreshPatterns();

                    _currentPatternIsBase = _currentSelection.CurrentPatternIndex == 0;

                    SetInspectorMode(InspectorModes.Pattern);
                });
            });


            _sequencesPanel.Query<Button>("AddButton").ForEach(button =>
            {
                button.RegisterCallback<ClickEvent>((evt) =>
                {
                    Debug.Log("new pattern");
                    if (evt.currentTarget is not Button btn) return;
                    var newPattern = new AnyPattern();
                    newPattern.Init();
                    int trackIndex = Int32.Parse(btn.tooltip);
                    var thisTrack = CurrentSong.Sections[0].tracks[trackIndex];
                    thisTrack.patterns.Add(newPattern);
                    _currentSelection.CurrentPatternIndex = thisTrack.patterns.Count - 1;

                    AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);

                    AnysongSequencesView.RefreshPatterns();
                    HandleSequencesLogic();
                    HandlePatternsLogic();
                });
            });

            _sequencesPanel.Query<Button>("RemoveButton").ForEach(button =>
            {
                button.RegisterCallback<ClickEvent>((evt) =>
                {
                    Debug.Log("remove pattern");
                    if (evt.currentTarget is not Button btn) return;

                    int trackIndex = Int32.Parse(btn.tooltip);
                    var thisTrack = CurrentSong.Sections[0].tracks[trackIndex];
                    thisTrack.patterns.Remove(thisTrack.patterns[_currentSelection.CurrentPatternIndex]);
                    _currentSelection.CurrentPatternIndex -= 1;
                    AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);

                    AnysongSequencesView.RefreshPatterns();
                    HandleSequencesLogic();
                    HandlePatternsLogic();
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


        AnySelection GetSelectionFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            var selection = new AnySelection();
            selection.CurrentStepIndex = Int32.Parse(str[0]);
            selection.CurrentTrackIndex = Int32.Parse(str[1]);
            selection.CurrentPatternIndex = Int32.Parse(str[2]);

            selection.CurrentSectionTrack = CurrentSong.Sections[0].tracks[selection.CurrentTrackIndex];
            selection.CurrentPattern =
                selection.CurrentSectionTrack.patterns[selection.CurrentPatternIndex];
            selection.CurrentStep = selection.CurrentPattern.steps[selection.CurrentStepIndex];
            selection.CurrentSongTrack = CurrentSong.Tracks[selection.CurrentTrackIndex];

            var song = new SerializedObject(CurrentSong);
            var section = song.FindProperty("Sections").GetArrayElementAtIndex(0);
            var track = section.FindPropertyRelative("tracks")
                .GetArrayElementAtIndex(selection.CurrentTrackIndex);
            var pattern = track.FindPropertyRelative("patterns")
                .GetArrayElementAtIndex(selection.CurrentPatternIndex);
            var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(selection.CurrentStepIndex);

            selection.CurrentPatternProperty = pattern;
            selection.CurrentStepProperty = step;
            selection.CurrentSongTrackProperty = song.FindProperty("Tracks")
                .GetArrayElementAtIndex(selection.CurrentTrackIndex);

            return selection;
        }


        public static AnyPatternStep GetPatternStepFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            int stepIndex = Int32.Parse(str[0]);
            int trackIndex = Int32.Parse(str[1]);
            int patternIndex = Int32.Parse(str[2]);
            return CurrentSong.Sections[0].tracks[trackIndex].patterns[patternIndex].steps[stepIndex];
        }

        void SetInspectorMode(InspectorModes inspectorMode)
        {
            switch (inspectorMode)
            {
                case InspectorModes.Sections:
                    break;
                case InspectorModes.Pattern:
                    AnysongInspectorView.DrawPattern(_currentSelection.CurrentPatternProperty, _currentPatternIsBase,
                        null);
                    HandlePatternUtilsLogic();
                    break;
                case InspectorModes.Track:
                    AnysongInspectorView.DrawTrack(_currentSelection.CurrentSongTrackProperty);

                    break;
                case InspectorModes.Step:
                    AnysongInspectorView.DrawStep(_currentSelection.CurrentStepProperty,
                        AnysongSequencesView.RefreshPatterns);
                    HandleStepUtilsLogic();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inspectorMode), inspectorMode, null);
            }
        }


        void HandlePatternUtilsLogic()
        {
            _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) =>
            {
                CopyPattern(_currentSelection.CurrentPattern);
            });
            _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) => { PastePattern(); });
        }

        void HandleStepUtilsLogic()
        {
            _inspectorPanel.Q<Button>("CopyButton").RegisterCallback<ClickEvent>((evt) =>
            {
                CopyStep(_currentSelection.CurrentStep);
            });
            _inspectorPanel.Q<Button>("PasteButton").RegisterCallback<ClickEvent>((evt) => { PasteStep(); });
        }


        void CopyStep(AnyPatternStep step)
        {
            Debug.Log("Copy step");
            _stepCopy = step.Clone();
        }

        void PasteStep()
        {
            Debug.Log("paste step");
            for (var i = 0; i < _currentSelection.CurrentPattern.steps.Count; i++)
            {
                var patternStep = _currentSelection.CurrentPattern.steps[i];
                if (patternStep == _currentSelection.CurrentStep)
                {
                    _currentSelection.CurrentPattern.steps[i] = _stepCopy;
                    break;
                }
            }

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


        AnyPattern _patternCopy;


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
    }
}