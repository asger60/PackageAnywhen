using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnywhenConductor))]
public class AnywhenConductorInspector : UnityEditor.Editor
{

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();


        var rootNoteLabel = new TextField("Root note")
        {
            isReadOnly = true,
        };
        rootNoteLabel.BindProperty(serializedObject.FindProperty("_rootNote"));

        var scaleLabel = new TextField("Scale")
        {
            isReadOnly = true,
        };
            
        scaleLabel.BindProperty(serializedObject.FindProperty("_currentAnywhenScale"));


        
        

        inspector.Add(rootNoteLabel);
        inspector.Add(scaleLabel);
       // inspector.Add(stringField);
        return inspector;
    }

    
}