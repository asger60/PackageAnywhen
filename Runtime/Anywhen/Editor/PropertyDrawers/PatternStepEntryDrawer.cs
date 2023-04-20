using PackageAnywhen.Runtime.Anywhen;
using UnityEditor;
using UnityEngine;

// IngredientDrawer
[CustomPropertyDrawer(typeof(StepPattern.PatternStepEntry))]
public class PatternStepEntryDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        position.width = 300;
        EditorGUI.BeginProperty(position, label, property);
        // Draw label
        var shortLabel = new GUIContent(label.ToString().Replace("Element", ""));
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), shortLabel);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        // Calculate rects
        position.x -= 60;
        var isOnRect = new Rect(position.x, position.y, 60, 20);
        var accentRect = new Rect(position.x + 20, position.y, 60, 20);
        var noteRect = new Rect(position.x + 60, position.y, 30, 20);

        var chordRect = new Rect(position.x + 120, position.y, 60, 20);
        
        var weightRect = new Rect(position.x + 260, position.y, 45, 20);
        

        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(isOnRect, property.FindPropertyRelative("noteOn"), GUIContent.none);
        EditorGUI.PropertyField(accentRect, property.FindPropertyRelative("accent"), GUIContent.none);
        EditorGUI.PropertyField(noteRect, property.FindPropertyRelative("note"), GUIContent.none);
        
        EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("stepWeight"), GUIContent.none);
        
        
        
        EditorGUI.PropertyField(chordRect, property.FindPropertyRelative("chord"), GUIContent.none);
        
        



        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

}