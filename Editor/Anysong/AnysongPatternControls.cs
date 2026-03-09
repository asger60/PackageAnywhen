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


        public static VisualElement Draw()
        {
            VisualElement controls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            _patternNoteOfsetUp = new Button
            {
                text = "↑"
            };
            _patternNoteOfsetDown = new Button
            {
                text = "↓"
            };
            _monoButton = new Button
            {
                text = "Mono"
            };
            _polyButton = new Button
            {
                text = "Poly"
            };

            _pitchButton = new Button() { text = "Pitch" };
            _velocityButton = new Button() { text = "Velocity" };
            _durationButton = new Button() { text = "Length" };

            _pitchButton.clicked += OnEditPitch;
            _velocityButton.clicked += OnEditVelocity;
            _durationButton.clicked += OnEditLength;
            _patternNoteOfsetUp.clicked += OnOffsetUp;
            _patternNoteOfsetDown.clicked += OnOffsetDown;
            _monoButton.clicked += OnMono;
            _polyButton.clicked += OnPoly;


            _patternNoteOfsetUp.AddToClassList("progression-select-button");
            _patternNoteOfsetDown.AddToClassList("progression-select-button");
            _polyButton.AddToClassList("progression-select-button");
            _monoButton.AddToClassList("progression-select-button");
            _pitchButton.AddToClassList("progression-select-button");
            _velocityButton.AddToClassList("progression-select-button");
            _durationButton.AddToClassList("progression-select-button");

            RefreshMonoPolyButtons();
            RefreshEditButtons();

            controls.Add(_pitchButton);
            controls.Add(_velocityButton);
            controls.Add(_durationButton);
            controls.Add(MakeSpacer());


            controls.Add(_monoButton);
            controls.Add(_polyButton);
            controls.Add(MakeSpacer());


            controls.Add(_patternNoteOfsetUp);
            controls.Add(_patternNoteOfsetDown);
            controls.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);

            return controls;
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
            AnysongPatternView.SetMonoOrPoly(false);
            RefreshMonoPolyButtons();
        }

        private static void OnPoly()
        {
            AnysongPatternView.SetMonoOrPoly(true);
            RefreshMonoPolyButtons();
        }

        static void RefreshEditButtons()
        {
            _pitchButton.RemoveFromClassList("editing");
            _velocityButton.RemoveFromClassList("editing");
            _durationButton.RemoveFromClassList("editing");
            if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteLength)
                _durationButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NotePitch)
                _pitchButton.AddToClassList("editing");
            else if (AnysongPatternView.CurrentEditMode == AnysongPatternView.EditModes.NoteVelocity)
                _velocityButton.AddToClassList("editing");
        }

        static void RefreshMonoPolyButtons()
        {
            if (AnysongPatternView.IsPolyfonic)
                _polyButton.AddToClassList("editing");
            else
                _polyButton.RemoveFromClassList("editing");
            if (!AnysongPatternView.IsPolyfonic)
                _monoButton.AddToClassList("editing");
            else
                _monoButton.RemoveFromClassList("editing");
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
        }
    }
}