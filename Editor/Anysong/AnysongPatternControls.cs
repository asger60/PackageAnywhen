using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public static class AnysongPatternControls
    {
        static Button _monoButton;
        static Button _polyButton;
        static Button _patternNoteOfsetUp;
        static Button _patternNoteOfsetDown;
        static Button _pitchButton;
        static Button _velocityButton;
        static Button _durationButton;
        private static Button _chanceButton;
        static Button _weightButton;


        public static VisualElement Draw()
        {
            VisualElement controls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            bool isPercussion = AnysongEditorWindow.CurrentSelection.currentSongTrackSettings.instrument is AnywhenSampleInstrument sampleInstrument &&
                                sampleInstrument.clipSelectType == AnywhenSampleInstrument.ClipSelectType.Percussion;

            if (!isPercussion)
            {
                _patternNoteOfsetUp = new Button { text = "↑" };
                _patternNoteOfsetDown = new Button { text = "↓" };
                _monoButton = new Button { text = "Mono" };
                _polyButton = new Button { text = "Poly" };
                _patternNoteOfsetUp.clicked += OnOffsetUp;
                _patternNoteOfsetDown.clicked += OnOffsetDown;
                _monoButton.clicked += OnMono;
                _polyButton.clicked += OnPoly;
                _patternNoteOfsetUp.AddToClassList("progression-select-button");
                _patternNoteOfsetDown.AddToClassList("progression-select-button");
                _polyButton.AddToClassList("progression-select-button");
                _monoButton.AddToClassList("progression-select-button");
            }

            _pitchButton = new Button() { text = "Pitch" };
            _velocityButton = new Button() { text = "Velocity" };
            _durationButton = new Button() { text = "Length" };
            _chanceButton = new Button() { text = "Chance" };
            _weightButton = new Button() { text = "Weight" };

            _pitchButton.clicked += OnEditPitch;
            _velocityButton.clicked += OnEditVelocity;
            _durationButton.clicked += OnEditLength;
            _chanceButton.clicked += OnChance;
            _weightButton.clicked += OnWeights;


            _pitchButton.AddToClassList("progression-select-button");
            _velocityButton.AddToClassList("progression-select-button");
            _durationButton.AddToClassList("progression-select-button");
            _chanceButton.AddToClassList("progression-select-button");
            _weightButton.AddToClassList("progression-select-button");

            RefreshMonoPolyButtons();
            RefreshEditButtons();

            controls.Add(_pitchButton);
            controls.Add(_velocityButton);
            controls.Add(_durationButton);
            controls.Add(_chanceButton);
            controls.Add(_weightButton);


            if (!isPercussion)
            {
                controls.Add(MakeSpacer());
                controls.Add(_monoButton);
                controls.Add(_polyButton);
                controls.Add(MakeSpacer());
                controls.Add(_patternNoteOfsetUp);
                controls.Add(_patternNoteOfsetDown);
                if (AnysongEditorWindow.CurrentSelection.currentSongTrackSettings.voices == 1)
                    OnMono();
                else
                    OnPoly();
            }

            controls.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);


            return controls;
        }

        private static void OnWeights()
        {
            AnysongPatternView.SetEditMode(AnysongPatternView.EditModes.NoteWeights);
            RefreshEditButtons();
        }

        private static void OnChance()
        {
            AnysongPatternView.SetEditMode(AnysongPatternView.EditModes.NoteChance);
            RefreshEditButtons();
        }

        static VisualElement MakeSpacer()
        {
            return new VisualElement { style = { width = 20 } };
        }

        static void OnEditPitch()
        {
            AnysongPatternView.SetEditMode(AnysongPatternView.EditModes.NotePitch);
            RefreshEditButtons();
        }

        static void OnEditVelocity()
        {
            AnysongPatternView.SetEditMode(AnysongPatternView.EditModes.NoteVelocity);
            RefreshEditButtons();
        }

        static void OnEditLength()
        {
            AnysongPatternView.SetEditMode(AnysongPatternView.EditModes.NoteLength);
            RefreshEditButtons();
        }

        private static void OnOffsetUp() => AnysongPatternView.OffsetPatternNoteIndex(-1);
        private static void OnOffsetDown() => AnysongPatternView.OffsetPatternNoteIndex(1);

        private static void OnMono()
        {
            AnysongPatternView.SetPolyfonic(false);
            RefreshMonoPolyButtons();
        }

        private static void OnPoly()
        {
            AnysongPatternView.SetPolyfonic(true);
            RefreshMonoPolyButtons();
        }

        static void RefreshEditButtons()
        {
            _pitchButton.RemoveFromClassList("editing");
            _velocityButton.RemoveFromClassList("editing");
            _durationButton.RemoveFromClassList("editing");
            _chanceButton.RemoveFromClassList("editing");
            _weightButton.RemoveFromClassList("editing");

            if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteLength)
                _durationButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NotePitch)
                _pitchButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteVelocity)
                _velocityButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteChance)
                _chanceButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteWeights)
                _weightButton.AddToClassList("editing");
        }

        static void RefreshMonoPolyButtons()
        {
            if (AnysongPatternView.IsPolyfonic)
                _polyButton?.AddToClassList("editing");
            else
                _polyButton?.RemoveFromClassList("editing");
            if (!AnysongPatternView.IsPolyfonic)
                _monoButton?.AddToClassList("editing");
            else
                _monoButton?.RemoveFromClassList("editing");
        }

        private static void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            if (_patternNoteOfsetUp != null) _patternNoteOfsetUp.clicked -= OnOffsetUp;
            if (_patternNoteOfsetDown != null) _patternNoteOfsetDown.clicked -= OnOffsetDown;
            if (_monoButton != null) _monoButton.clicked -= OnMono;
            if (_polyButton != null) _polyButton.clicked -= OnPoly;
            if (_pitchButton != null) _pitchButton.clicked -= OnEditPitch;
            if (_velocityButton != null) _velocityButton.clicked -= OnEditVelocity;
            if (_durationButton != null) _durationButton.clicked -= OnEditLength;
            if (_chanceButton != null) _chanceButton.clicked -= OnChance;
            if (_weightButton != null) _weightButton.clicked -= OnWeights;
        }
    }
}