using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AnysongEditorWindow : EditorWindow
{
    public static AnySongObject CurrentSong { get; private set; }
    public static AnySection CurrentSection { get; private set; }

    public static AnySongTrack CurrentSongTrack { get; private set; }

    public static AnySectionTrack CurrentSectionTrack { get; private set; }
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
        {
            CurrentSong = Selection.activeObject as AnySongObject;
            return;
        }
        if (CurrentSection == null && CurrentSong != null)
            CurrentSection = CurrentSong.Sections[0];

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical();
        GUILayout.Label(CurrentSong.name, EditorStyles.boldLabel);
        GUILayout.Space(10);


        EditorGUILayout.BeginHorizontal();
        HandleSongTracks();


        EditorGUILayout.BeginVertical();


        HandleSections();
        GUILayout.BeginHorizontal();

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


        EditorGUILayout.EndVertical();


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
        EditorGUILayout.LabelField("Sections");

        EditorGUILayout.BeginHorizontal();


        for (int i = 0; i < CurrentSong.Sections.Count; i++)
        {
            GUI.backgroundColor = CurrentSection == CurrentSong.Sections[i] ? _hilightedColor : Color.white;

            if (GUILayout.Button("Section " + i))
            {
                CurrentSection = CurrentSong.Sections[i];
                SetInspectorMode(InspectorModes.Sections);
                CurrentSectionTrack = CurrentSection.tracks[0];
                CurrentPattern = CurrentSectionTrack.patterns[0];
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


    void HandlePatterns()
    {
        EditorGUILayout.BeginHorizontal();
        if (CurrentSectionTrack == null) return;
        for (var i = 0; i < CurrentSectionTrack.patterns.Count; i++)
        {
            GUI.backgroundColor = CurrentPattern == CurrentSectionTrack.patterns[i]
                ? _hilightedColor
                : Color.white;
            if (GUILayout.Button("Pattern " + i))
            {
                SetInspectorMode(InspectorModes.Pattern);
                CurrentPattern = CurrentSectionTrack.patterns[i];
            }
        }

        GUI.backgroundColor = Color.white;


        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newPattern = new AnyPattern();
            newPattern.Init();
            CurrentSectionTrack.patterns.Add(newPattern);
            CurrentPattern = CurrentSectionTrack.patterns[^1];
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            CurrentSectionTrack.patterns.Remove(CurrentPattern);
            CurrentPattern = CurrentSectionTrack.patterns[^1];
        }

        EditorGUILayout.EndHorizontal();
    }


    void HandleSongTracks()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(200));

        EditorGUILayout.LabelField("Tracks", GUILayout.Width(154));


        for (var trackIndex = 0; trackIndex < CurrentSong.Tracks.Count; trackIndex++)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = CurrentSongTrack == CurrentSong.Tracks[trackIndex] ? _hilightedColor : Color.white;

            var thisTrack = CurrentSong.Tracks[trackIndex];
            var instrumentName = thisTrack.instrument != null ? thisTrack.instrument.name : "no instrument selected";
            if (GUILayout.Button(instrumentName, GUILayout.Width(200)))
            {
                CurrentSectionTrack = CurrentSection.tracks[trackIndex];
                CurrentSongTrack = thisTrack;

                CurrentPattern = CurrentSectionTrack.patterns[0];
                CurrentStep = null;
                SetInspectorMode(InspectorModes.Track);
                _currentTrackIndex = trackIndex;
            }


            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
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
        EditorGUILayout.EndHorizontal();
    }

    void HandleSteps()
    {
        if (CurrentPattern == null) return;


        GUILayout.BeginHorizontal();
        for (int step = 0; step < CurrentPattern.steps.Count; step++)
        {
            var thisStep = CurrentPattern.steps[step];
            var color = Color.white;
            if (thisStep.noteOff) color = Color.red;
            if (thisStep.noteOn) color = Color.green;

            if (thisStep.noteOn && thisStep.isChord) color = Color.blue;

            GUI.backgroundColor = thisStep == CurrentStep ? _hilightedColor : Color.white;
            GUI.color = color;

            if (GUILayout.Button(new GUIContent(thisStep.notes[0].ToString(), (step + 1).ToString())))
            {
                CurrentStep = thisStep;
                SetInspectorMode(InspectorModes.Step);
            }
        }

        var hover = GUI.tooltip;
        if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.A)
        {
            var thisStep = CurrentPattern.steps[Int32.Parse(hover) - 1];
            CurrentStep = thisStep;
            thisStep.noteOn = !thisStep.noteOn;

            thisStep.isChord = false;
            SetInspectorMode(InspectorModes.Step);
            Repaint();
        }

        if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
        {
            var thisStep = CurrentPattern.steps[Int32.Parse(hover) - 1];
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
            var thisStep = CurrentPattern.steps[Int32.Parse(hover) - 1];
            CurrentStep = thisStep;
            for (var i = 0; i < thisStep.notes.Count; i++)
            {
                thisStep.notes[i]--;
            }

            SetInspectorMode(InspectorModes.Step);
            Repaint();
        }

        if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.C)
        {
            var thisStep = CurrentPattern.steps[Int32.Parse(hover) - 1];
            CurrentStep = thisStep;
            thisStep.noteOn = !thisStep.noteOn;
            thisStep.isChord = true;
            thisStep.notes = new List<int> { 0, 2, 4 };
            SetInspectorMode(InspectorModes.Step);
            Repaint();
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
                CurrentPattern.DrawInspector();
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
        CurrentSectionTrack.patterns[0] = PatternCopy;
        CurrentPattern = PatternCopy;
        PatternCopy = null;
        window.ConsoleLog("pasted pattern");
    }
}