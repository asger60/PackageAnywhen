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
        public static AnySongObject CurrentSong { get; private set; }
        public static AnySection CurrentSection { get; private set; }

        public static AnySongTrack CurrentSongTrack { get; private set; }

        public static AnySectionTrack CurrentSectionTrack { get; private set; }

        public static AnyPatternStep CurrentStep { get; private set; }

        public static AnyPatternStep CurrentHoverStep { get; private set; }

        public static SerializedProperty CurrentStepProperty { get; private set; }

        private Event _currentEvent;

        public static Color ColorHilight1 = new Color(0.8f, 0.9f, 0, 1);
        public static Color ColorHilight2 = new Color(0.8f, 0.4f, 0, 1);
        public static Color ColorHilight3 = new Color(0.2f, 0.4f, 0.8f, 1);
        public static Color ColorGrey = new Color(0.35f, 0.3f, 0.3f, 1);

        private AnysongPlayer _currentRuntimeSongPlayer;
        
        
        public enum InspectorModes
        {
            Sections,
            Pattern,
            Track,
            Step
        }

        private int _currentTrackIndex;


        private InspectorModes _currentInspectorMode = InspectorModes.Sections;

        [MenuItem("Anywhen/Anysong Editor")]
        public static void ShowModuleWindow()
        {
            Object[] selection = Selection.GetFiltered(typeof(AnySongObject), SelectionMode.Assets);
            if (selection.Length > 0)
            {
                AnySongObject songObject = selection[0] as AnySongObject;
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
                }
            }
        }

        private VisualElement _sequencesPanel;
        private VisualElement _tracksPanel;
        private VisualElement _inspectorPanel;

        public void CreateGUI()
        {
            // Get a list of all sprites in the project
            rootVisualElement.Clear();
            if (CurrentSong == null)
            {
                CurrentSong = Selection.activeObject as AnySongObject;
                if (CurrentSong == null)
                {
                    CurrentSong = AssetDatabase.LoadAssetAtPath<AnySongObject>(EditorPrefs.GetString("AnyLoadedSong"));
                }
            }

            if (CurrentSection == null && CurrentSong != null) CurrentSection = CurrentSong.Sections[0];


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
                //var track = CurrentSong.Sections[0].tracks[i];
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

        private float _scroll;
        private void OnWheel(WheelEvent evt)
        {
            if (CurrentHoverStep == null) return;
            _scroll += evt.delta.y;
            CurrentHoverStep.rootNote = (int)_scroll;
            AnysongSequencesView.RefreshPatterns(CurrentSong.Sections[0]);
        }


        private void OnKeyDown(KeyDownEvent evt)
        {
            Debug.Log("pressed " + evt.keyCode);
            if (CurrentHoverStep == null) return;
            if (evt.keyCode == KeyCode.C)
            {
                //CurrentHoverStep.IsChord = !CurrentHoverStep.IsChord;
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                CurrentHoverStep.rootNote++;
                AnysongSequencesView.RefreshPatterns(CurrentSong.Sections[0]);
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                CurrentHoverStep.rootNote--;
                AnysongSequencesView.RefreshPatterns(CurrentSong.Sections[0]);
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
                        _currentTrackIndex = btn.tabIndex;
                        CurrentSongTrack = CurrentSong.Tracks[btn.tabIndex];
                        SetInspectorMode2(InspectorModes.Track);
                    }
                });
            });

            _tracksPanel.Q<Button>("AddTrackButton").RegisterCallback((ClickEvent ev) =>
            {
                var newTrack = new AnySongTrack();
                newTrack.Init();
                CurrentSong.Tracks.Add(newTrack);
                foreach (var section in CurrentSong.Sections)
                {
                    section.AddSongTrack(newTrack);
                }

                CreateGUI();
            });

            _tracksPanel.Q<Button>("RemoveTrackButton").RegisterCallback((ClickEvent ev) =>
            {
                foreach (var section in CurrentSong.Sections)
                {
                    section.RemoveSongTrack(CurrentSong.Tracks[_currentTrackIndex]);
                }

                CurrentSong.Tracks.RemoveAt(_currentTrackIndex);
                _currentTrackIndex = 0;
                CreateGUI();
            });
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
            var thisStep = GetPatternStepFromTooltip(btn.tooltip);
            CurrentHoverStep = thisStep;
            _scroll = CurrentHoverStep.rootNote;
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            if (evt.currentTarget is not Button btn) return;
            CurrentHoverStep = null;
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.currentTarget is not Button btn) return;
            CurrentStepProperty = GetSerializedPropertyFromTooltip(btn.tooltip);
            CurrentStep = GetPatternStepFromTooltip(btn.tooltip);

            SetInspectorMode2(InspectorModes.Step);
            if (evt.button == 0)
            {
                CurrentStep.noteOn = !CurrentStep.noteOn;
            }
        }


        SerializedProperty GetSerializedPropertyFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            int stepIndex = Int32.Parse(str[0]);
            int trackIndex = Int32.Parse(str[1]);
            int patternIndex = Int32.Parse(str[2]);

            var song = new SerializedObject(CurrentSong);
            var section = song.FindProperty("Sections").GetArrayElementAtIndex(0);
            var track = section.FindPropertyRelative("tracks").GetArrayElementAtIndex(trackIndex);
            var pattern = track.FindPropertyRelative("patterns").GetArrayElementAtIndex(patternIndex);
            var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(stepIndex);

            return step;
        }

        public static AnyPatternStep GetPatternStepFromTooltip(string tooltip)
        {
            var str = tooltip.Split("-");
            int stepIndex = Int32.Parse(str[0]);
            int trackIndex = Int32.Parse(str[1]);
            int patternIndex = Int32.Parse(str[2]);
            return CurrentSong.Sections[0].tracks[trackIndex].patterns[patternIndex].steps[stepIndex];
        }

        void SetInspectorMode2(InspectorModes inspectorMode)
        {
            switch (inspectorMode)
            {
                case InspectorModes.Sections:
                    break;
                case InspectorModes.Pattern:
                    break;
                case InspectorModes.Track:
                    AnysongInspectorView.DrawTrack(CurrentSongTrack);
                    _inspectorPanel.Q<Slider>("Volume").RegisterCallback((ClickEvent ev) =>
                    {
                        if (ev.currentTarget is Slider slider)
                        {
                            Debug.Log(slider?.value);
                            CurrentSongTrack.volume = slider.value;
                        }
                    });
                    break;
                case InspectorModes.Step:
                    AnysongInspectorView.DrawStep(CurrentStepProperty,
                        () =>
                        {
                            AnysongSequencesView.RefreshPatterns(CurrentSong.Sections[0]);
                            Debug.Log("did update step");
                        });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inspectorMode), inspectorMode, null);
            }
        }


        public static AnysongEditorWindow Create(Object asset)
        {
            var window = CreateWindow<AnysongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
            return window;
        }


      
        private string _consoleMessage;
        private double _consoleLogTime;


        void HandleTrackSequences()
        {
            EditorGUILayout.LabelField("Sequences", GUILayout.Height(16));
            if (CurrentSection != null)
            {
                for (var i = 0; i < CurrentSection.tracks.Count; i++)
                {
                    var track = CurrentSection.tracks[i];
                    HandleTrackPatterns(track);
                    HandleSteps(track.EditorCurrentPattern, i);
                    GUILayout.Space(8);
                }
            }
        }

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


        void HandleSections()
        {
            EditorGUILayout.LabelField("Sections");

            EditorGUILayout.BeginHorizontal();


            for (int i = 0; i < CurrentSong.Sections.Count; i++)
            {
                GUI.backgroundColor = CurrentSection == CurrentSong.Sections[i] ? ColorHilight1 : Color.white;

                if (GUILayout.Button("Section " + i))
                {
                    CurrentSection = CurrentSong.Sections[i];
                    SetInspectorMode(InspectorModes.Sections);
                    CurrentSectionTrack = CurrentSection.tracks[0];
                    CurrentSectionTrack.EditorCurrentPattern = CurrentSectionTrack.patterns[0];
                    _currentTrackIndex = 0;
                }
            }

            GUI.backgroundColor = Color.white;


            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                var newSection = new AnySection();
                newSection.Init(CurrentSong.Tracks);
                CurrentSong.Sections.Add(newSection);
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                CurrentSong.Sections.Remove(CurrentSection);
                CurrentSection = CurrentSong.Sections[^1];
            }

            EditorGUILayout.EndHorizontal();
        }


        void HandleTrackPatterns(AnySectionTrack track)
        {
            if (CurrentSectionTrack == null) return;

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < track.patterns.Count; i++)
            {
                GUI.backgroundColor = CurrentSectionTrack.EditorCurrentPattern == track.patterns[i]
                    ? ColorHilight1
                    : Color.white;

                if (GUILayout.Button("Pattern " + i))
                {
                    SetInspectorMode(InspectorModes.Pattern);
                    track.EditorCurrentPattern = track.patterns[i];
                }
            }

            GUI.backgroundColor = Color.white;


            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                var newPattern = new AnyPattern();
                newPattern.Init();
                track.patterns.Add(newPattern);
                CurrentSectionTrack.EditorCurrentPattern = track.patterns[^1];
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                track.patterns.Remove(CurrentSectionTrack.EditorCurrentPattern);
                CurrentSectionTrack.EditorCurrentPattern = track.patterns[^1];
            }

            EditorGUILayout.EndHorizontal();
        }



        enum CurrentEditMode
        {
            NoteOn,
            NoteOff,
            Inspect,
        }

        private CurrentEditMode _currentEditMode = CurrentEditMode.NoteOn;

        void HandleSteps(AnyPattern pattern, int row)
        {
            
        }

        public static AnyPattern PatternCopy;

        void HandleInspector()
        {
        }

        void SetInspectorMode(InspectorModes newMode)
        {
            _currentInspectorMode = newMode;
            GUI.FocusControl(null);
        }


        public static void ClearCurrentPattern()
        {
            var newPattern = new AnyPattern();
            newPattern.Init();
            CurrentSectionTrack.EditorCurrentPattern = newPattern;
        }

        public static void CopyCurrentPattern()
        {
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            PatternCopy = CurrentSectionTrack.EditorCurrentPattern.Clone();
            window.ConsoleLog("copied pattern");
        }

        public static void PastePattern()
        {
            AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
            CurrentSectionTrack.patterns[0] = PatternCopy;
            CurrentSectionTrack.EditorCurrentPattern = PatternCopy;
            PatternCopy = null;
            window.ConsoleLog("pasted pattern");
        }
    }
}