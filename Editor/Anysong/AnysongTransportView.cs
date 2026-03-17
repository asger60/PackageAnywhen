using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


public static class AnysongTransportView
{
    private static VisualElement _parent;
    private static Button _playButton;

    public static void Clear()
    {
        _parent = null;
        _playButton = null;
    }

    public static void Draw(VisualElement parent, AnysongObject currentSong)
    {
        _parent = parent;
        var headerElement = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
            }
        };
        var controlsElement = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
            }
        };

        parent.Clear();
        _parent.Add(headerElement);
        _parent.Add(controlsElement);

        _playButton = new Button()
        {
            name = "PlayButton",
            text = "Play",
            style = { width = 100 }
        };
        _playButton.AddToClassList("transport-play-button");
        controlsElement.Add(_playButton);


        controlsElement.Add(Spacer());

        var song = new SerializedObject(currentSong);
        var tempoProperty = song.FindProperty("tempo");

        var volumeProperty = song.FindProperty("songVolume");
        var tempoPopertyField = new PropertyField(tempoProperty);
        tempoPopertyField.BindProperty(tempoProperty);
        tempoPopertyField.style.width = 300;
        controlsElement.Add(tempoPopertyField);


        var songVolumeField = new PropertyField(volumeProperty);
        songVolumeField.BindProperty(volumeProperty);
        songVolumeField.style.width = 300;
        controlsElement.Add(songVolumeField);

        var intensitySlider = new Slider(0, 1)
        {
            style = { width = 300 },
            direction = SliderDirection.Horizontal,
            name = "TestIntensitySlider",
            label = "Intensity",
            value = 1,
            showInputField = true,
        };

        controlsElement.Add(Spacer());
        controlsElement.Add(intensitySlider);

        VisualElement snapShotControlElement = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                width = 400,
                flexShrink = 0,
            }
        };
        _snapshotButtonA = new Button
        {
            text = "A"
        };
        _snapshotButtonA.clicked += () => ToggleSnapShot(false);
        _snapshotButtonA.AddToClassList("snapshot-button");
        _snapshotButtonB = new Button
        {
            text = "B"
        };
        _snapshotButtonB.AddToClassList("snapshot-button");
        _snapshotButtonB.clicked += () => ToggleSnapShot(true);
        _snapShotLerpSlider = new Slider(0, 1)
        {
            style =
            {
                width = 200
            }
        };

        snapShotControlElement.Add(_snapshotButtonA);
        snapShotControlElement.Add(_snapShotLerpSlider);
        snapShotControlElement.Add(_snapshotButtonB);

        controlsElement.Add(snapShotControlElement);
    }

    static VisualElement Spacer(float width = 20)
    {
        var spacer = new VisualElement
        {
            style =
            {
                width = width
            }
        };
        return spacer;
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
        }
        else
        {
            _snapshotButtonA.AddToClassList("triggered");
            _snapshotButtonB.RemoveFromClassList("triggered");
        }
    }
}