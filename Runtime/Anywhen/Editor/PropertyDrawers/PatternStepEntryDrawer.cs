using Anywhen;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        var isOnRect = new Rect(position.x, position.y, 80, 20);
        var accentRect = new Rect(position.x + 100, position.y, 60, 20);

        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(isOnRect, property.FindPropertyRelative("noteOn"));
        EditorGUI.PropertyField(accentRect, property.FindPropertyRelative("accent"), GUIContent.none);

        //var isOnLabel = new GUIContent("x");

        //if (isOnArray.arraySize == 0)
        //{
        //    isOnArray.arraySize = 16;
        //}
//
        //if (accentOnArray.arraySize == 0)
        //{
        //    accentOnArray.arraySize = 16;
        //}

        //EditorGUI.LabelField(isOnRect, "NoteOn");
        //EditorGUI.LabelField(accentRect, "Accent");
        //for (int i = 0; i < 16; i++)
        //{
        //    isOnRect.y += 20;
        //    accentRect.y += 20;
        //    stepIndexRect.y += 20;
        //    
        //    EditorGUI.LabelField(stepIndexRect, i.ToString());
        //    isOnLabel = new GUIContent(i.ToString());
        //    EditorGUI.PropertyField(isOnRect, isOnArray.GetArrayElementAtIndex(i), GUIContent.none);
//
        //    EditorGUI.PropertyField(accentRect, accentOnArray.GetArrayElementAtIndex(i), GUIContent.none);
//
        //}


        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    //public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //{
    //    return 370f;
    //}
}