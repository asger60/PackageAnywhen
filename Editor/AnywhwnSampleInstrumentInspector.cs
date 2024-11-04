using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnywhenSampleInstrument))]
public class AnywhenSampleInstrumentInspector : UnityEditor.Editor
{
    private AnywhenSampleInstrument _anywhenSampleInstrument;

    private void OnEnable()
    {
        _anywhenSampleInstrument = target as AnywhenSampleInstrument;
    }


    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        var previewButton = new Button
        {
            text = "Preview"
        };
        previewButton.clicked += () => { _anywhenSampleInstrument.PreviewSound(); };
        var deleteButton = new Button
        {
            text = "Delete AudioClips",
            style = { backgroundColor = new StyleColor(Color.red)}
        };
        deleteButton.clicked += () => { _anywhenSampleInstrument.DeleteAudioCLips(); };

        InspectorElement.FillDefaultInspector(inspector, serializedObject, this);

        inspector.Add(previewButton);
        inspector.Add(deleteButton);
        return inspector;
    }
}