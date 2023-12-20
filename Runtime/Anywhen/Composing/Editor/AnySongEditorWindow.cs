using System;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AnysongEditorWindow : EditorWindow
{
    public static AnySongObject CurrentSong { get; private set; }
    public static AnySection CurrentSection { get; private set; }
    public static AnyTrack CurrentTrack { get; private set; }
    public static AnyPattern CurrentPattern { get; private set; }
    public static AnyPatternStep CurrentStep { get; private set; }

    private Color _hilightedColor = new Color(0.8f, 0.9f, 1);

    private enum InspectorModes
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
                window.Show();
            }
        }
    }


    public static AnysongEditorWindow Create(Object asset)
    {
        var window = CreateWindow<AnysongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
        return window;
    }


    void OnGUI()
    {
        if (CurrentSong == null)
            CurrentSong = Selection.activeObject as AnySongObject;
        if (CurrentSection == null && CurrentSong != null)
            CurrentSection = CurrentSong.Sections[0];

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.BeginVertical();
        GUILayout.Label(CurrentSong.name, EditorStyles.boldLabel);
        GUILayout.Space(10);


        HandleSections();
        GUILayout.BeginHorizontal();
        HandleTracks();
        GUILayout.BeginVertical();
        GUILayout.Space(20 + (_currentTrackIndex * 21));
        HandlePatterns();
        HandleSteps();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(10);


        GUILayout.Space(20);
        EditorGUILayout.EndVertical();
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical(GUILayout.Width(300));

        HandleInspector();
        HandleConsole();

        EditorGUILayout.EndVertical();


        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(CurrentSong);
        }
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


    void HandleSections()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sections", GUILayout.Width(100));


        for (int i = 0; i < CurrentSong.Sections.Count; i++)
        {
            GUI.backgroundColor = CurrentSection == CurrentSong.Sections[i] ? _hilightedColor : Color.white;

            if (GUILayout.Button("Section " + i))
            {
                CurrentSection = CurrentSong.Sections[i];
                SetInspectorMode(InspectorModes.Sections);
            }
        }

        GUI.backgroundColor = Color.white;


        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newSection = new AnySection();
            newSection.Init();
            CurrentSong.Sections.Add(newSection);
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            CurrentSong.Sections.Remove(CurrentSection);
            CurrentSection = CurrentSong.Sections[^1];
        }

        EditorGUILayout.EndHorizontal();
    }


    void HandlePatterns()
    {
        EditorGUILayout.BeginHorizontal();

        for (var i = 0; i < CurrentSection.tracks[_currentTrackIndex].patterns.Count; i++)
        {
            GUI.backgroundColor = CurrentPattern == CurrentSection.tracks[_currentTrackIndex].patterns[i]
                ? _hilightedColor
                : Color.white;
            if (GUILayout.Button("Pattern " + i))
            {
                SetInspectorMode(InspectorModes.Pattern);
                CurrentPattern = CurrentSection.tracks[_currentTrackIndex].patterns[i];
            }
        }

        GUI.backgroundColor = Color.white;


        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newPattern = new AnyPattern();
            newPattern.Init();
            CurrentSection.tracks[_currentTrackIndex].patterns.Add(newPattern);
            CurrentPattern = CurrentSection.tracks[_currentTrackIndex].patterns[^1];
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            CurrentSection.tracks[_currentTrackIndex].patterns.Remove(CurrentPattern);
            CurrentPattern = CurrentSection.tracks[_currentTrackIndex].patterns[^1];
        }

        EditorGUILayout.EndHorizontal();
    }


    void HandleTracks()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tracks", GUILayout.Width(154));
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newTrack = new AnyTrack();
            newTrack.Init();
            CurrentSection.tracks.Add(newTrack);
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            CurrentSection.tracks.RemoveAt(_currentTrackIndex);
        }

        EditorGUILayout.EndHorizontal();


        for (var track = 0; track < CurrentSection.tracks.Count; track++)
        {
            EditorGUILayout.BeginHorizontal();

            var thisTrack = CurrentSection.tracks[track];
            var instrumentName = thisTrack.instrument != null ? thisTrack.instrument.name : "no instrument selected";
            if (GUILayout.Button(instrumentName, GUILayout.Width(200)))
            {
                CurrentTrack = CurrentSection.tracks[track];
                CurrentPattern = CurrentTrack.patterns[0];
                CurrentStep = null;
                SetInspectorMode(InspectorModes.Track);
                _currentTrackIndex = track;
            }


            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndHorizontal();
    }

    void HandleSteps()
    {
        if (CurrentPattern == null) return;
        if (Event.current.isKey && Event.current.keyCode == KeyCode.A)
        {
            _aDown = Event.current.type == EventType.KeyDown ? true : false;
        }

        GUILayout.BeginHorizontal();
        for (int step = 0; step < CurrentPattern.steps.Count; step++)
        {
            var thisStep = CurrentPattern.steps[step];
            var color = Color.white;
            if (thisStep.noteOff) color = Color.red;
            if (thisStep.noteOn) color = Color.green;
            GUI.backgroundColor = thisStep == CurrentStep
                ? _hilightedColor
                : Color.white;

            GUI.color = color;
            if (GUILayout.Button(""))
            {
                if (_aDown)
                {
                    thisStep.noteOn = !thisStep.noteOn;
                }

                CurrentStep = thisStep;
                SetInspectorMode(InspectorModes.Step);
            }
        }

        GUILayout.EndHorizontal();
    }

    public static AnyPattern PatternCopy;

    void HandleInspector()
    {
        GUILayout.Label("Inspector", EditorStyles.boldLabel);

        switch (_currentInspectorMode)
        {
            case InspectorModes.Sections:
                break;
            case InspectorModes.Pattern:
                CurrentPattern.DrawInspector();
                break;
            case InspectorModes.Track:
                CurrentTrack.DrawInspector();
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
        CurrentPattern = newPattern;
    }

    public static void CopyCurrentPattern()
    {
        AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));

        PatternCopy = CurrentPattern.Clone();
        window.ConsoleLog("copied pattern");
    }

    public static void PastePattern()
    {
        AnysongEditorWindow window = (AnysongEditorWindow)GetWindow(typeof(AnysongEditorWindow));
        CurrentTrack.patterns[0] = PatternCopy;
        CurrentPattern = PatternCopy;
        PatternCopy = null;
        window.ConsoleLog("pasted pattern");
    }
}