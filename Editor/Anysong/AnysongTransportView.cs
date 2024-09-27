using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongTransportView
    {
        private static VisualElement _parent;
        private static Button _playButton;

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
            

            Label songTitle = new Label(currentSong.name)
            {
                style =
                {
                    textShadow = new StyleTextShadow(new TextShadow()),
                    marginLeft = 3,
                    marginTop = 3
                }
            };

            headerElement.Add(songTitle);
            //_parent.Add(Spacer(100));


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
            controlsElement.Add(Spacer());

            _playButton = new Button()
            {
                name = "PlayButton",
                text = "Play",
                style = { width = 100}
            };

            controlsElement.Add(_playButton);
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
        }
    }
}