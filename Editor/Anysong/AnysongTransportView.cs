using Anysong;
using Anywhen;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


public static class AnysongTransportView
{
    private static VisualElement _parent;
    private static Button _playButton;
    static SerializedObject _song;

    public static void Clear()
    {
        _parent = null;
        _playButton = null;
    }

    public static void Draw(VisualElement parent, AnysongObject currentSong)
    {
        _parent = parent;

        var visualizerContainer = _parent.Q<VisualElement>("Visualizer");



        _playButton = _parent.Q<Button>("PlayButton");

        _playButton.AddToClassList("transport-play-button");
        
        _song = new SerializedObject(currentSong);
        
        var tempoProperty = _song.FindProperty("tempo");
        var tempoSlider = _parent.Q<SliderInt>("TempoSlider");
        tempoSlider.BindProperty(tempoProperty);
        tempoSlider.RegisterValueChangedCallback(evt => { AnysongEditorWindow.SetBPM(evt.newValue); });


        var intensitySlider = _parent.Q<Slider>("IntensitySlider");
        intensitySlider.RegisterValueChangedCallback(evt => { AnysongEditorWindow.SetTestIntensity(evt.newValue); });


        _snapshotButtonA = _parent.Q<Button>("SnapshotButtonA");
        _snapshotButtonA.clicked += () => ToggleSnapShot(false);
        _snapshotButtonA.AddToClassList("snapshot-button");
        _snapshotButtonB = _parent.Q<Button>("SnapshotButtonB");
        _snapshotButtonB.AddToClassList("snapshot-button");
        _snapshotButtonB.clicked += () => ToggleSnapShot(true);
        _snapShotLerpSlider = _parent.Q<Slider>("SnapshotSlider");
        
        _snapShotLerpSlider.RegisterValueChangedCallback(evt =>
        {
            float newValue = evt.newValue;
            AnywhenSnapshotEditor.ApplyBlend(AnysongEditorWindow.CurrentSong.snapshotA, AnysongEditorWindow.CurrentSong.snapshotB,
                _song, newValue);
        });


        

        var visualizer = new OscilloscopeElement();
        visualizerContainer.Add(visualizer);
        visualizer.style.width = 340;
        visualizer.style.height = 80;
    }



    public static void RefreshPlaybuttonState(bool isPlaying)
    {
        _playButton.text = isPlaying ? "Stop" : "Play";
        if (isPlaying)
            _playButton.AddToClassList("triggered");
        else
            _playButton.RemoveFromClassList("triggered");
    }

    static bool _isSnapShotEnabled = false;
    private static Slider _snapShotLerpSlider;
    private static Button _snapshotButtonA, _snapshotButtonB;

    static void ToggleSnapShot(bool state)
    {
        _isSnapShotEnabled = state;
        _snapShotLerpSlider.SetValueWithoutNotify(_isSnapShotEnabled ? 1f : 0);
        if (_isSnapShotEnabled)
        {
            _snapshotButtonB.AddToClassList("triggered");
            _snapshotButtonA.RemoveFromClassList("triggered");
            AnywhenSnapshotEditor.CaptureSnapshot(_song, ref AnysongEditorWindow.CurrentSong.snapshotB);
        }
        else
        {
            _snapshotButtonA.AddToClassList("triggered");
            _snapshotButtonB.RemoveFromClassList("triggered");
            AnywhenSnapshotEditor.CaptureSnapshot(_song, ref AnysongEditorWindow.CurrentSong.snapshotA);
        }
    }
}