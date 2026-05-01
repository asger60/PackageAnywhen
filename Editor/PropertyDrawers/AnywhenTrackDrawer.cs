using System.IO;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnywhenTrackTypeAttribute))]
public class AnywhenTrackDrawer : PropertyDrawer
{
    private const string AssetPath = "Assets/Anywhen/AnywhenTrackTypes.asset";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool isString = property.propertyType == SerializedPropertyType.String;
        bool isInt = property.propertyType == SerializedPropertyType.Integer;

        if (!isString && !isInt)
        {
            EditorGUI.HelpBox(position, "[AnywhenTrackType] only works on string or int fields.", MessageType.Error);
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<AnywhenTrackTypeAsset>(AssetPath);

        if (!asset)
        {
            asset = CreateAsset(AssetPath);
        }

        if (!asset)
        {
            EditorGUI.HelpBox(position, $"Could not create AnywhenTrackTypeAsset at: {AssetPath}", MessageType.Error);
            return;
        }

        var items = asset.items;

        // Build display list — prepend a "(None)" option
        var displayOptions = new List<string> { "(None)" };
        displayOptions.AddRange(items);

        // Find current index
        int currentIndex = 0;
        if (isString)
        {
            string current = property.stringValue;
            currentIndex = items.IndexOf(current) + 1; // +1 offset for (None)
        }
        else if (isInt)
        {
            currentIndex = property.intValue;
            if (currentIndex < 0 || currentIndex >= displayOptions.Count)
            {
                currentIndex = 0; // Default to (None) if out of range
            }
        }

        EditorGUI.BeginProperty(position, label, property);

        // Reserve the last 60px for the "Edit..." button
        float buttonWidth = 60f;
        float spacing = 4f;
        Rect dropdownRect = new Rect(position.x, position.y, position.width - buttonWidth - spacing, position.height);
        Rect buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, position.height);

        int newIndex = EditorGUI.Popup(dropdownRect, label.text, currentIndex, displayOptions.ToArray());

        if (newIndex != currentIndex)
        {
            if (isString)
            {
                property.stringValue = newIndex == 0 ? "" : items[newIndex - 1];
            }
            else if (isInt)
            {
                property.intValue = newIndex;
            }
        }

        // "Edit..." button opens the settings window
        if (GUI.Button(buttonRect, "Edit"))
        {
            AnywhenTrackTypeEditorWindow.Open(asset);
        }

        EditorGUI.EndProperty();
    }

    private AnywhenTrackTypeAsset CreateAsset(string path)
    {
        var asset = ScriptableObject.CreateInstance<AnywhenTrackTypeAsset>();
        asset.items.Add("(None)");
        asset.items.Add("Rhythm");
        asset.items.Add("Bass");
        asset.items.Add("Lead");
        asset.items.Add("Synth short");
        asset.items.Add("Synth long");
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }
}