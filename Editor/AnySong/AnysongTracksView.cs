using System;
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.AnySong
{
    public static class AnysongTracksView
    {
        private static VisualElement _parent;

        public static void Draw(VisualElement parent, AnysongObject currentSong)
        {
            _parent = parent;
            parent.Clear();
            parent.Add(new Label("Tracks"));


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
                        flexDirection = FlexDirection.Row
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
                    style = { width = 20 }
                };




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
                    style = { width = 20 }
                };
                
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
                        width = new StyleLength(170),
                        
                        height = 40,
                    }
                };


                trackElement.Add(button);
                parent.Add(trackElement);
                parent.Add(spacer);
            }

         
            parent.Add(AnysongEditorWindow.CreateAddRemoveButtons());
        }

        static void CheckMute(SerializedProperty property)
        {
            Debug.Log(property.boolValue);
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
                var state = AnysongEditorWindow.GetCurrentSection().tracks[index].isSolo;
                btn.style.backgroundColor = state ? AnysongEditorWindow.ColorHilight2 : StyleKeyword.Null;
                index++;
            });
        }
        
        static void UpdateMuteButtons()
        {
            int index = 0;
            _parent.Query<Button>("MuteButton").ForEach((btn) =>
            {
                var state = AnysongEditorWindow.GetCurrentSection().tracks[index].isMuted;
                btn.style.backgroundColor = state ? AnysongEditorWindow.ColorGreyDark : StyleKeyword.Null;
                btn.text = "M";
                index++;
            });
        }

        static void MuteTrackAtIndex(string indexString)
        {
            var track = AnysongEditorWindow.CurrentSong.Sections[0].tracks[Int32.Parse(indexString)];
            track.isMuted = !track.isMuted;
            UpdateMuteButtons();
        }

        static void SoloTrackAtIndex(string indexString)
        {
            var track = AnysongEditorWindow.GetCurrentSection().tracks[Int32.Parse(indexString)];

            bool unSolo = track.isSolo;

            foreach (var sectionTrack in AnysongEditorWindow.GetCurrentSection().tracks)
            {
                sectionTrack.isSolo = false;
                if (sectionTrack == track && !unSolo)
                {
                    sectionTrack.isSolo = true;
                }

                if (unSolo)
                {
                    sectionTrack.isMuted = false;
                }
                else
                {
                    sectionTrack.isMuted = sectionTrack != track;
                }
            }

            UpdateSoloButtons();
            UpdateMuteButtons();
        }
    }
}