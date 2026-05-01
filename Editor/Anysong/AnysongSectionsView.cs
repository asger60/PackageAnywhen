using System;
using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Anysong
{
    public static class AnysongSectionsView
    {
        private static VisualElement _parent;

        public static void Clear()
        {
            _parent = null;
        }

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

            var lockButton = new Button
            {
                name = "SectionLockButton",
                text = "L",
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row,
                }
            };
            lockButton.AddToClassList("section-lock-button");
         

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
                };

                button.AddToClassList("section-edit-button");
                if (i == currentSelectionIndex)
                    button.AddToClassList("editing");


                sectionElement.Add(button);
                parent.Add(sectionElement);
                parent.Add(spacer);
            }

            parent.Add(AnysongEditorWindowNew.CreateAddRemoveButtons());
            RefreshSectionLocked();
        }

        public static void RefreshSectionLocked()
        {
            _parent.Query<Button>("SectionLockButton").ForEach(button =>
            {
                if (AnysongEditorWindowNew.IsSectionLocked)
                    button.AddToClassList("triggered");
                else
                    button.RemoveFromClassList("triggered");
            });
        }



        public static void SetPlayingSectionIndex(int currentSectionIndex)
        {
            _parent.Query<Button>("SectionButton").ForEach(button =>
            {
                int thisIndex = Int32.Parse(button.tooltip);
                if (thisIndex == currentSectionIndex)
                {
                    button.AddToClassList("triggered");
                }
                else
                {
                    button.RemoveFromClassList("triggered");
                }
            });
        }
    }
}