using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongProgressionsView
    {
        public static void Draw(VisualElement parent, AnysongObject currentSong)
        {
            parent.Clear();
            parent.Add(new Label());
            var spacer = new ToolbarSpacer
            {
                style = { height = 8 }
            };


            for (var i = 0; i < currentSong.Tracks.Count; i++)
            {
                var trackElement = new VisualElement
                {
                    style =
                    {
                        alignItems = Align.Center,
                        flexDirection = FlexDirection.Row,
                        height = 45,
                        width = 60
                    }
                };
                var button = new Button
                {
                    name = "ProgressionButton",
                    tooltip = 0 + "-" + i + "-" + 0,
                    text = "P",
                    style =
                    {
                        height = 40,
                        width = 60
                    }
                };


                trackElement.Add(button);
                parent.Add(trackElement);
            }
        }
    }
}