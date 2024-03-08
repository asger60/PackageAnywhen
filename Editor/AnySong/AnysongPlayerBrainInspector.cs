using Anywhen.Composing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnysongPlayerBrain))]
public class AnysongPlayerBrainInspector : UnityEditor.Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        var songPlayersProperty = serializedObject.FindProperty("songPlayers");
        var playersPropertyField = new PropertyField(songPlayersProperty);
        playersPropertyField.BindProperty(songPlayersProperty);
        
        
        var transitionProperty = serializedObject.FindProperty("transitionType");
        var transitionPropertyField = new PropertyField(transitionProperty);
        //transitionPropertyField.BindProperty(transitionProperty);
        
        var globalIntensity = serializedObject.FindProperty("globalIntensity");
        var globalIntensityField = new PropertyField(globalIntensity);
        globalIntensityField.BindProperty(globalIntensity);

        // propertyField.RegisterValueChangeCallback((ev) => { didUpdate?.Invoke(); });

        inspector.Add(playersPropertyField);
        
        inspector.Add(globalIntensityField);
        
        inspector.Add(transitionPropertyField);
        


        var playButtonsHolder = new VisualElement()
        {
            style = { flexDirection = FlexDirection.Row }
        };

        for (int i = 0; i < songPlayersProperty.arraySize; i++)
        {
            var songPlayer = songPlayersProperty.GetArrayElementAtIndex(i).objectReferenceValue as AnysongPlayer;

            if (songPlayer != null)
            {
                var playButton = new Button()
                {
                    text = songPlayer.songObject.name
                };
                playButton.RegisterCallback<ClickEvent>((evt) =>
                {
                    AnysongPlayerBrain.TransitionTo(songPlayer, AnysongPlayerBrain.TransitionTypes.Instant);
                });
                playButtonsHolder.Add(playButton);
            }
        }

        inspector.Add(playButtonsHolder);

        return inspector;
    }
}