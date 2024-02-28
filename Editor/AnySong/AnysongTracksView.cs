using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongTracksView
    {
        public static void Draw(VisualElement parent, AnysongObject currentSong)
        {
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
                    tabIndex = i,
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

            var buttons = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };


            var addButton = new Button
            {
                name = "AddTrackButton",
                text = "+"
            };


            var deleteButton = new Button
            {
                name = "RemoveTrackButton",
                text = "-"
            };

            buttons.Add(addButton);
            buttons.Add(deleteButton);
            parent.Add(buttons);

            
        }
    }
}