using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class AnySongEditorWindow : EditorWindow
{
    [FormerlySerializedAs("anySong")] public AnySongObject currentSong;
    private Color _hilightedColor = new Color(0.8f, 0.9f, 1);

    private enum InspectorModes
    {
        Sections,
        Pattern,
        Track,
        Step
    }

    private int _selectedPatternStep;
    private int _currentTrack;
    bool _aDown = false;

    private InspectorModes _currentInspectorMode = InspectorModes.Sections;

    [MenuItem("Anywhen/AnySong Editor")]
    public static void ShowModuleWindow()
    {
        Object[] selection = Selection.GetFiltered(typeof(AnySongObject), SelectionMode.Assets);
        if (selection.Length > 0)
        {
            AnySongObject songObject = selection[0] as AnySongObject;
            if (songObject != null)
            {
                AnySongEditorWindow window = (AnySongEditorWindow)GetWindow(typeof(AnySongEditorWindow));
                window.currentSong = songObject;
                window.Show();
            }
        }
    }

    public static AnySongEditorWindow Create(Object asset)
    {
        var window = CreateWindow<AnySongEditorWindow>($"{asset.name} | {asset.GetType().Name}");
        return window;
    }


    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        GUILayout.Label(currentSong.name, EditorStyles.boldLabel);
        GUILayout.Space(10);

        HandleSections();
        HandleTracks();
        HandlePatterns();

        GUILayout.Space(10);


        GUILayout.Space(20);
        HandleInspector();
        HandleConsole();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentSong);
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

    int _currentSection = 0;

    void HandleSections()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sections", GUILayout.Width(100));


        for (int i = 0; i < currentSong.Sections.Count; i++)
        {
            GUI.backgroundColor = i == _currentSection ? _hilightedColor : Color.white;

            if (GUILayout.Button("Section " + i))
            {
                _currentSection = i;
                SetInspectorMode(InspectorModes.Sections);
            }
        }

        GUI.backgroundColor = Color.white;


        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newSection = new AnySection();
            newSection.Init();
            currentSong.Sections.Add(newSection);
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            currentSong.Sections.RemoveAt(_currentSection);
            _currentSection = 0;
        }

        EditorGUILayout.EndHorizontal();
    }

    int _currentPattern = 0;

    void HandlePatterns()
    {
        /*
        AnySection.MusicVariation musicVariation = anySection.musicVariations[_currentVariation];
        if (musicVariation.patterns == null || musicVariation.patterns.Count == 0)
        {
            Debug.Log("add pattern");
            anySection.musicVariations[_currentVariation] =
                new AnySection.MusicVariation(anySection.instruments);
            musicVariation = anySection.musicVariations[_currentVariation];
        }

        _currentPattern = Mathf.Clamp(_currentPattern, 0, musicVariation.patterns.Count - 1);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Patterns", GUILayout.Width(100));

        for (var i = 0; i < musicVariation.patterns.Count; i++)
        {
            GUI.backgroundColor = i == _currentPattern ? _hilightedColor : Color.white;
            if (GUILayout.Button("Pattern " + i))
            {
                SetInspectorMode(InspectorModes.Pattern);
                _currentPattern = i;
            }
        }

        GUI.backgroundColor = Color.white;


        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            anySection.musicVariations[_currentVariation].patterns
                .Add(new AnySection.MusicVariation.MusicPattern(anySection.instruments));
            _currentPattern = anySection.musicVariations[_currentVariation].patterns.Count - 1;
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            anySection.musicVariations[_currentVariation].patterns.RemoveAt(_currentPattern);
            _currentPattern = 0;
        }

        EditorGUILayout.EndHorizontal();
        */
    }


    void HandleTracks()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tracks", GUILayout.Width(EditorGUIUtility.currentViewWidth - 53));
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            var newTrack = new AnySection.AnyTrack();
            newTrack.Init();
            currentSong.Sections[_currentSection].tracks.Add(newTrack);
        }

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            currentSong.Sections[_currentSection].tracks.RemoveAt(_currentTrack);
        }

        EditorGUILayout.EndHorizontal();

        //var pattern = anySection.musicVariations[_currentSection].patterns[_currentPattern];
        //if (pattern.tracks == null || pattern.tracks.Count == 0)
        //{
        //    
        //    anySection.musicVariations[_currentSection].patterns[_currentPattern] =
        //        new AnySection.MusicVariation.MusicPattern(anySection.instruments);
//
        //    pattern = anySection.musicVariations[_currentSection].patterns[_currentPattern];
        //}
//
        //if (pattern.tracks.Count < anySection.instruments.Length)
        //{
        //    pattern.tracks.Add(new AnySection.MusicVariation.AnyTrack());
        //}


        for (var track = 0; track < currentSong.Sections[_currentSection].tracks.Count; track++)
        {
            EditorGUILayout.BeginHorizontal();

            //if (pattern.tracks[track].steps == null || pattern.tracks[track].steps.Count == 0)
            //{
            //    anySection.musicVariations[_currentSection].patterns[_currentPattern] =
            //        new AnySection.MusicVariation.MusicPattern(anySection.instruments);
            //    pattern = anySection.musicVariations[_currentSection].patterns[_currentPattern];
            //}

            var thisTrack = currentSong.Sections[_currentSection].tracks[track];
            var instrumentName = thisTrack.instrument != null ? thisTrack.instrument.name : "no instrument selected";
            if (GUILayout.Button(instrumentName, GUILayout.Width(100)))
            {
                SetInspectorMode(InspectorModes.Track);
            }


            //if (Event.current.isKey && Event.current.keyCode == KeyCode.A)
            //{
            //    _aDown = Event.current.type == EventType.KeyDown ? true : false;
            //}
//
            //for (int step = 0; step < thisTrack.steps.Count; step++)
            //{
            //    var thisStep = thisTrack.steps[step];
            //    var color = Color.white;
            //    if (thisStep.noteOff) color = Color.red;
            //    if (thisStep.noteOn) color = Color.green;
            //    GUI.backgroundColor = step == _selectedPatternStep && _currentTrack == track
            //        ? _hilightedColor
            //        : Color.white;
//
//
            //    GUI.color = color;
            //    if (GUILayout.Button(""))
            //    {
            //        if (_aDown)
            //        {
            //            thisStep.noteOn = !thisStep.noteOn;
            //        }
//
            //        _selectedPatternStep = step;
            //        _currentTrack = track;
            //        SetInspectorMode(InspectorModes.Step);
            //    }
            //}

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }

    //private AnySection.MusicVariation.MusicPattern _patternCopy;

    void HandleInspector()
    {
        /*
        GUILayout.Label("Inspector", EditorStyles.boldLabel);

        switch (_currentInspectorMode)
        {
            case InspectorModes.Variation:
                break;
            case InspectorModes.Pattern:

                var pattern = anySection.musicVariations[_currentVariation].patterns[_currentPattern];
                pattern.patternFrequency = EditorGUILayout.IntField("Pattern frequency", pattern.patternFrequency);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy", GUILayout.Width(60)))
                {
                    _patternCopy = pattern.Clone();
                    ConsoleLog("copied pattern");
                }

                if (_patternCopy != null)
                {
                    if (GUILayout.Button("Paste", GUILayout.Width(60)))
                    {
                        anySection.musicVariations[_currentVariation].patterns[_currentPattern] = _patternCopy;
                        ConsoleLog("pasted pattern");
                    }
                }

                GUI.color = Color.red;
                GUILayout.Space(20);
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    anySection.musicVariations[_currentVariation].patterns[_currentPattern] =
                        new AnySection.MusicVariation.MusicPattern(anySection.instruments);
                }

                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                break;
            case InspectorModes.Track:
                break;
            case InspectorModes.Step:
                var step = anySection.musicVariations[_currentVariation].patterns[_currentPattern]
                    .tracks[_currentTrack].steps[_selectedPatternStep];

                step.noteOn = EditorGUILayout.Toggle("Note On", step.noteOn);
                step.noteOff = EditorGUILayout.Toggle("Note Off", step.noteOff);
                step.duration = EditorGUILayout.FloatField("Duration", step.duration);
                step.offset = EditorGUILayout.Slider("Nudge", step.offset, -1, 1);
                step.velocity = EditorGUILayout.Slider("Velocity", step.velocity, 0, 1);
                step.expression = EditorGUILayout.Slider("Expression", step.expression, 0, 1);
                step.mixWeight = EditorGUILayout.FloatField("Weight", step.mixWeight);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Notes", GUILayout.Width(150));
                for (int i = 0; i < step.notes.Count; i++)
                {
                    step.notes[i] = EditorGUILayout.IntField("", step.notes[i], GUILayout.Width(20));
                }

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    step.notes.Add(new int());
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    step.notes.RemoveAt(step.notes.Count - 1);
                }

                EditorGUILayout.EndHorizontal();
                step.noteRandom = EditorGUILayout.IntField("Note random", step.noteRandom);


                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        */
    }

    void SetInspectorMode(InspectorModes newMode)
    {
        _currentInspectorMode = newMode;
        GUI.FocusControl(null);
    }
}