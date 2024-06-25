using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongSectionsView
    {
        private static VisualElement _parent;

        public static void Draw(VisualElement parent, AnysongObject currentSong)
        {
            _parent = parent;
            parent.Clear();
            var headerElement = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            parent.Add(headerElement);


            var spacer = new ToolbarSpacer
            {
                style = { width = 8 }
            };


            for (var i = 0; i < currentSong.Sections.Count; i++)
            {
                var sectionElement = new VisualElement
                {
                    style =
                    {
                        alignItems = Align.Center,
                        flexDirection = FlexDirection.Row
                    }
                };
                

                var button = new Button
                {
                    name = "SectionButton",
                    tooltip = i.ToString(),
                    text = "Section " + i.ToString(),
                    style =
                    {
                        width = new StyleLength(170),
                        height = 20,
                    }
                };


                sectionElement.Add(button);
                parent.Add(sectionElement);
                parent.Add(spacer);
            }
            
            var lockButton = new Button
            {
                name = "SectionLockButton",
                text = "Lock ",
                style =
                {
                    width = new StyleLength(60),
                    height = 20,
                }
            };
            

            parent.Add(AnysongEditorWindow.CreateAddRemoveButtons());
            
            parent.Add(lockButton);
        }
    }
}