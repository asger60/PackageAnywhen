using System;
using Anywhen.Composing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


public static class AnysongTracksView
{
    private static VisualElement _parent;

    public static void Draw(VisualElement parent, AnysongObject currentSong)
    {
        _parent = parent;
        parent.Clear();


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
                }
            };
            var soundControlElement = new VisualElement
            {
                style =
                {
                    width = 20,
                    minWidth = 20,
                },
            };

            var muteButton = new Button()
            {
                text = "M",
                name = "MuteButton",
                tooltip = i.ToString(),
            };
            muteButton.AddToClassList("track-mix-button");


            muteButton.RegisterCallback<ClickEvent>((evt) =>
            {
                if (evt.currentTarget is not Button btn) return;
                MuteTrackAtIndex(btn.tooltip);
            });


            var soloButton = new Button
            {
                text = "S",
                name = "SoloButton",
                tooltip = i.ToString(),
            };
            soloButton.AddToClassList("track-mix-button");

            soloButton.RegisterCallback<ClickEvent>((evt) =>
            {
                if (evt.currentTarget is not Button btn) return;
                SoloTrackAtIndex(btn.tooltip);
            });

            soundControlElement.Add(muteButton);
            soundControlElement.Add(soloButton);


            trackElement.Add(soundControlElement);


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
                    width = new StyleLength(210),
                    height = 40,
                }
            };
            button.AddToClassList("track-edit-button");

            trackElement.Add(button);
            parent.Add(trackElement);
            parent.Add(spacer);
        }


        parent.Add(AnysongEditorWindow.CreateAddRemoveButtons());
    }


    public static void UpdateMuteSoleState()
    {
        UpdateMuteButtons();
        UpdateSoloButtons();
    }

    static void UpdateSoloButtons()
    {
        int index = 0;
        _parent.Query<Button>("SoloButton").ForEach((btn) =>
        {
            var state = AnysongEditorWindow.CurrentSong.Tracks[index].IsSolo;
            //btn.style.backgroundColor = state ? AnysongEditorWindow.ColorHilight2 : StyleKeyword.Null;
            if (state)
                btn.AddToClassList("track-mix-button-solo");
            else
                btn.RemoveFromClassList("track-mix-button-solo");
            index++;
        });
    }

    static void UpdateMuteButtons()
    {
        int index = 0;
        _parent.Query<Button>("MuteButton").ForEach((btn) =>
        {
            var state = AnysongEditorWindow.CurrentSong.Tracks[index].IsMuted;
            if (state)
                btn.AddToClassList("track-mix-button-muted");
            else
                btn.RemoveFromClassList("track-mix-button-muted");

            btn.text = "M";
            index++;
        });
    }

    static void MuteTrackAtIndex(string indexString)
    {
        var track = AnysongEditorWindow.CurrentSong.Tracks[Int32.Parse(indexString)];
        track.IsMuted = !track.IsMuted;
        UpdateMuteButtons();
    }

    static void SoloTrackAtIndex(string indexString)
    {
        var track = AnysongEditorWindow.CurrentSong.Tracks[Int32.Parse(indexString)];

        bool unSolo = track.IsSolo;

        foreach (var sectionTrack in AnysongEditorWindow.CurrentSong.Tracks)
        {
            sectionTrack.IsSolo = false;
            if (sectionTrack == track && !unSolo)
            {
                sectionTrack.IsSolo = true;
            }

            if (unSolo)
            {
                sectionTrack.IsMuted = false;
            }
            else
            {
                sectionTrack.IsMuted = sectionTrack != track;
            }
        }

        UpdateSoloButtons();
        UpdateMuteButtons();
    }
}