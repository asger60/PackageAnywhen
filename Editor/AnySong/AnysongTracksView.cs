using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongTracksView
    {
        public static void Draw(VisualElement parent, AnysongObject currentSong)
        {
            parent.Clear();
            parent.Add(new Label("Tracks"));
            var spacer = new ToolbarSpacer
            {
                style = { height = 8 }
            };

            for (var i = 0; i < currentSong.Tracks.Count; i++)
            {
                var thisTrack = currentSong.Tracks[i];
                var instrumentName =
                    thisTrack.instrument != null ? thisTrack.instrument.name : "no instrument selected";

                var button = new Button
                {
                    name = "TrackButton",
                    tooltip = 0 + "-" + i + "-" + 0,
                    text = instrumentName,
                    style =
                    {
                        height = 40,
                        //backgroundColor = CurrentSongTrack == CurrentSong.Tracks[i] ? _hilightedColor : Color.black
                    }
                };


                parent.Add(button);
                parent.Add(spacer);
            }

            parent.Add(AnysongEditorWindow.CreateAddRemoveButtons());
        }
    }
}