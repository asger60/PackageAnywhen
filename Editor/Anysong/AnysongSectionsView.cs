using System;
using System.Drawing;
using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongSectionsView
    {
        private static VisualElement _parent;

        public static void Draw(VisualElement parent, AnysongObject currentSong, int currentSelectionIndex)
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
                    text = "Section " + i,
                    style =
                    {
                        width = new StyleLength(170),
                        height = 20,
                        backgroundColor = i == currentSelectionIndex
                            ? AnysongEditorWindow.ColorGreyDark
                            : AnysongEditorWindow.ColorGreyDefault,
                    }
                };


                sectionElement.Add(button);
                parent.Add(sectionElement);
                parent.Add(spacer);
            }

            var lockButton = new Button
            {
                name = "SectionLockButton",
                text = "Lock",
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row,
                    width = new StyleLength(35),
                    height = 20,
                    backgroundColor = IsSectionLocked()
                        ? AnysongEditorWindow.ColorGreyDark
                        : AnysongEditorWindow.ColorGreyDefault,
                }
            };

            parent.Add(AnysongEditorWindow.CreateAddRemoveButtons());

            var lockElement = new VisualElement
            {
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            parent.Add(lockElement);
            lockElement.Add(lockButton);
        }

        public static bool IsSectionLocked()
        {
            return AnysongEditorWindow.CurrentSectionLockIndex > -1;
        }

        public static void HilightSection(int currentSectionIndex, int currentSelectionIndex)
        {
            _parent.Query<Button>("SectionButton").ForEach(button =>
            {
                int thisIndex = Int32.Parse(button.tooltip);
                if (thisIndex == currentSectionIndex)
                {
                    button.style.backgroundColor = AnysongEditorWindow.ColorHilight4;
                    button.style.color = AnysongEditorWindow.ColorGreyDark;
                }
                else
                {
                    button.style.color = UnityEngine.Color.white;
                    button.style.backgroundColor = thisIndex == currentSelectionIndex
                        ? AnysongEditorWindow.ColorGreyDark
                        : AnysongEditorWindow.ColorGreyDefault;
                }
            });
        }
    }
}