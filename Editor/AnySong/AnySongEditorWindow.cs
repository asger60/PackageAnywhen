using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Unity.VisualScripting;
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

        //public static AnyPattern CurrentPattern { get; private set; }
        public static AnyPatternStep CurrentStep { get; private set; }

        public static SerializedProperty CurrentStepProperty { get; private set; }

        private Event _currentEvent;

        public static Color ColorHilight1 = new Color(0.8f, 0.9f, 0, 1);
        public static Color ColorHilight2 = new Color(0.8f, 0.4f, 0, 1);
        public static Color ColorGrey = new Color(0.35f, 0.3f, 0.3f, 1);

        public enum InspectorModes
        {
            Sections,
            Pattern,
            Track,
            Step
        }

        private int _currentTrackIndex;
        bool _aDown = false;

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

            //var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            //rootVisualElement.Add(splitView);
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

                
                button.RegisterCallback((ClickEvent evt) =>
                {
                    Debug.Log("tap");
                    if (evt.currentTarget is Button btn)
                    {
                        var str = btn.tooltip.Split("-");
                        int stepIndex = Int32.Parse(str[0]);
                        int trackIndex = Int32.Parse(str[1]);
                        int patternIndex = Int32.Parse(str[2]);

                        var song = new SerializedObject(CurrentSong);
                        var section = song.FindProperty("Sections").GetArrayElementAtIndex(0);
                        var track = section.FindPropertyRelative("tracks").GetArrayElementAtIndex(trackIndex);
                        var pattern = track.FindPropertyRelative("patterns").GetArrayElementAtIndex(patternIndex);
                        var step = pattern.FindPropertyRelative("steps").GetArrayElementAtIndex(stepIndex);

                        CurrentStepProperty = step;

                        CurrentStep = CurrentSong.Sections[0].tracks[trackIndex].patterns[patternIndex]
                            .steps[stepIndex];
                        SetInspectorMode2(InspectorModes.Step);
                    }
                });
            });
        }
        
        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            Debug.Log($"Pointer LEAVE Event. Target: {(evt.target as VisualElement).name}");
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
                            AnysongSequencesView.Draw(_sequencesPanel, CurrentSong.Sections[0]);
                            HandleSequencesLogic();
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


        void OnGUI()
        {
            return;
            if (CurrentSong == null)
            {
                CurrentSong = Selection.activeObject as AnySongObject;
                if (CurrentSong == null)
                {
                    CurrentSong = AssetDatabase.LoadAssetAtPath<AnySongObject>(EditorPrefs.GetString("AnyLoadedSong"));
                }

                return;
            }


            if (CurrentSection == null && CurrentSong != null) CurrentSection = CurrentSong.Sections[0];


            _currentEvent = Event.current;


            EditorGUI.BeginChangeCheck();

            GUILayout.Label(CurrentSong.name, EditorStyles.boldLabel);
            GUILayout.Space(20);


            //three columns start
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            HandleSongTracks();
            EditorGUILayout.EndVertical();


            GUILayout.Space(10);
            GUILayout.BeginVertical();
            HandleTrackSequences();
            EditorGUILayout.EndVertical();


            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            HandleInspector();
            HandleConsole();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            //three columns end


            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(CurrentSong);
            }
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


        void HandleSongTracks()
        {
            EditorGUILayout.LabelField("Tracks", GUILayout.Width(154));

            for (var trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
            {
                GUI.backgroundColor =
                    CurrentSongTrack == CurrentSong.Tracks[trackIndex] ? ColorHilight1 : Color.white;

                var thisTrack = CurrentSong.Tracks[trackIndex];
                var instrumentName =
                    thisTrack.instrument != null ? thisTrack.instrument.name : "no instrument selected";
                if (GUILayout.Button(instrumentName, GUILayout.Width(200), GUILayout.Height(40)))
                {
                    CurrentSectionTrack = CurrentSection.tracks[trackIndex];
                    CurrentSongTrack = thisTrack;

                    CurrentSectionTrack.EditorCurrentPattern = CurrentSectionTrack.patterns[0];
                    CurrentStep = null;
                    SetInspectorMode(InspectorModes.Track);
                    _currentTrackIndex = trackIndex;
                }


                GUI.backgroundColor = Color.white;
                GUI.color = Color.white;


                GUILayout.Space(8);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                var newTrack = new AnySongTrack();
                newTrack.Init();
                CurrentSong.Tracks.Add(newTrack);
                foreach (var section in CurrentSong.Sections)
                {
                    section.AddSongTrack(newTrack);
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                foreach (var section in CurrentSong.Sections)
                {
                    section.RemoveSongTrack(CurrentSong.Tracks[_currentTrackIndex]);
                }

                CurrentSong.Tracks.RemoveAt(_currentTrackIndex);

                _currentTrackIndex = 0;
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
            if (CurrentSectionTrack?.EditorCurrentPattern == null) return;

            GUILayout.BeginHorizontal();
            for (int step = 0; step < pattern.steps.Count; step++)
            {
                var thisStep = pattern.steps[step];
                var color = Color.white;
                if (thisStep.noteOff) color = Color.red;
                if (thisStep.noteOn) color = Color.green;

                if (thisStep.noteOn && thisStep.isChord) color = Color.blue;

                GUI.backgroundColor = thisStep == CurrentStep ? ColorHilight1 : Color.white;
                GUI.color = color;

                Event e = Event.current;

                GUIContent buttonText = new GUIContent(thisStep.notes[0].ToString());

                Rect buttonRect = GUILayoutUtility.GetRect(buttonText, new GUIStyle("Button"));
                buttonRect.height = 40;


                if (e.isMouse && buttonRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        CurrentStep = thisStep;
                        SetInspectorMode(InspectorModes.Step);
                    }

                    if (e.type == EventType.MouseDrag)
                    {
                        thisStep.notes[0] -= (int)e.delta.y;
                    }
                }

                buttonRect.height = 20;
                GUI.Button(buttonRect, buttonText);

                //if (GUILayout.Button(new GUIContent(thisStep.notes[0].ToString(), (step + 1) + "." + (row))))
                //{
                //    CurrentStep = thisStep;
                //    SetInspectorMode(InspectorModes.Step);
                //    Debug.Log("mouse down");
                //    switch (_currentEditMode)
                //    {
                //        case CurrentEditMode.NoteOn:
                //            CurrentStep.ToggleNoteOn();
                //            break;
                //        case CurrentEditMode.NoteOff:
                //            break;
                //        default:
                //            throw new ArgumentOutOfRangeException();
                //    }
                //}
            }

            var toolTip = GUI.tooltip;

            if (string.IsNullOrEmpty(toolTip) || toolTip == "Error" || toolTip == "Warning")
            {
                GUILayout.EndHorizontal();
                GUI.color = Color.white;
                return;
            }

            var str = toolTip.Split(".");
            int thisButtonIndex = Int32.Parse(str[0]) - 1;
            int thisRowIndex = Int32.Parse(str[1]);

            if (thisRowIndex == row)
            {
                if (_currentEvent.isKey && _currentEvent.type == EventType.KeyDown &&
                    _currentEvent.keyCode == KeyCode.A)
                {
                    var thisStep = pattern.steps[thisButtonIndex];
                    CurrentStep = thisStep;
                    thisStep.noteOn = !thisStep.noteOn;

                    thisStep.isChord = false;
                    SetInspectorMode(InspectorModes.Step);
                    Repaint();
                }

                if (Event.current.isKey && Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.UpArrow)
                {
                    var thisStep = pattern.steps[thisButtonIndex];
                    CurrentStep = thisStep;
                    for (var i = 0; i < thisStep.notes.Count; i++)
                    {
                        thisStep.notes[i]++;
                    }

                    SetInspectorMode(InspectorModes.Step);
                    Repaint();
                }

                if (Event.current.isKey && Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.DownArrow)
                {
                    var thisStep = pattern.steps[thisButtonIndex];
                    CurrentStep = thisStep;
                    for (var i = 0; i < thisStep.notes.Count; i++)
                    {
                        thisStep.notes[i]--;
                    }

                    SetInspectorMode(InspectorModes.Step);
                    Repaint();
                }

                if (Event.current.isKey && Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.C)
                {
                    var thisStep = pattern.steps[thisButtonIndex];
                    CurrentStep = thisStep;
                    thisStep.noteOn = !thisStep.noteOn;
                    thisStep.isChord = true;
                    thisStep.notes = new List<int> { 0, 2, 4 };
                    SetInspectorMode(InspectorModes.Step);
                    Repaint();
                }
            }


            GUILayout.EndHorizontal();
            GUI.color = Color.white;
        }

        public static AnyPattern PatternCopy;

        void HandleInspector()
        {
            EditorGUILayout.LabelField("Inspector", GUILayout.Width(154));

            switch (_currentInspectorMode)
            {
                case InspectorModes.Sections:
                    break;
                case InspectorModes.Pattern:
                    AnyPatternEditor.DrawInspector(CurrentSectionTrack.EditorCurrentPattern);
                    //CurrentSectionTrack.EditorCurrentPattern.DrawInspector();
                    break;
                case InspectorModes.Track:
                    CurrentSongTrack.DrawInspector();
                    break;
                case InspectorModes.Step:
                    CurrentStep.DrawInspector();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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