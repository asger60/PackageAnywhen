using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;

public class AnywhenTrackTypeEditorWindow : EditorWindow
{
    private AnywhenTrackTypeAsset _typeAsset;
    private Vector2 _scroll;
    private string _newItem = "";

    public static void Open(AnywhenTrackTypeAsset typeAsset)
    {
        var window = GetWindow<AnywhenTrackTypeEditorWindow>("Edit Track Items");
        window._typeAsset = typeAsset;
        window.minSize = new Vector2(300, 200);
        window.Show();
    }

    private void OnGUI()
    {
        if (!_typeAsset)
        {
            EditorGUILayout.HelpBox("No asset selected.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(_typeAsset.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        var items = _typeAsset.items;
        int removeAt = -1;

        for (int i = 0; i < items.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            items[i] = EditorGUILayout.TextField(items[i]);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                removeAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeAt >= 0)
        {
            Undo.RecordObject(_typeAsset, "Remove Dropdown Item");
            items.RemoveAt(removeAt);
            EditorUtility.SetDirty(_typeAsset);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(4);

        // Add new item row
        EditorGUILayout.BeginHorizontal();
        _newItem = EditorGUILayout.TextField(_newItem);
        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_newItem));
        if (GUILayout.Button("Add", GUILayout.Width(48)))
        {
            Undo.RecordObject(_typeAsset, "Add Dropdown Item");
            items.Add(_newItem.Trim());
            _newItem = "";
            EditorUtility.SetDirty(_typeAsset);
            GUI.FocusControl(null);
            AssetDatabase.SaveAssets();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
    }
}