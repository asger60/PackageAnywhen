using System;
using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


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
            };

            button.AddToClassList("section-edit-button");
            if (i == currentSelectionIndex)
                button.AddToClassList("editing");


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

    public static void RefreshSectionLocked()
    {
        _parent.Query<Button>("SectionLockButton").ForEach(button =>
        {
            button.style.backgroundColor = IsSectionLocked()
                ? AnysongEditorWindow.ColorGreyDark
                : AnysongEditorWindow.ColorGreyDefault;
        });
    }

    static bool IsSectionLocked()
    {
        return AnysongEditorWindow.CurrentSong.SectionEditLock;
    }

    public static void HilightSection(int currentSectionIndex, int currentSelectionIndex)
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