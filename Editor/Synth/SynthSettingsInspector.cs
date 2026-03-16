using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Synth;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(AnywhenSynthPreset))]
public class SynthSettingsInspector : Editor
{
    private AnywhenSynthPreset _settingsObject;
    public AnywhenSynthPreset SettingsObject => _settingsObject;

    private bool _showOscilators => EditorPrefs.GetBool("Oscillators");
    private bool _showDevStuff => EditorPrefs.GetBool("Show Dev Stuff");
    private GUIStyle _sectionHeaderStyle;

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        _sectionHeaderStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.black
            },
        };
        _sectionHeaderStyle.fontSize = 20;


        EditorPrefs.SetBool("Show Dev Stuff",
            EditorGUILayout.Foldout(_showDevStuff, "Dev stuff", EditorStyles.foldoutHeader));
        if (_showDevStuff)
        {
            DrawDefaultInspector();
        }

        if (_settingsObject == null) _settingsObject = (AnywhenSynthPreset)this.target;
        if (_settingsObject == null) return;
        if (!_settingsObject.isInit)
        {
            if (GUILayout.Button("Init"))
            {
                InitPreset();
            }

            return;
        }

        DrawSectionHeader("Oscillators", _showOscilators);


        if (_showOscilators)
        {
            if (_settingsObject.oscillatorSettings.Length > 0)
            {
                foreach (var osc in _settingsObject.oscillatorSettings)
                {
                    SynthOSCInspector.Draw(this, osc);
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("add oscillator"))
            {
                CreateOscillator("oscillatorSettings", "Oscillator");
            }

            GUILayout.EndHorizontal();
        }


       

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(SettingsObject);
        }
    }

    void DrawUILine(Color color, int thickness = 1, int padding = 20)
    {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        rect.height = thickness;
        rect.y += padding / 2f;
        EditorGUI.DrawRect(rect, color);
    }

    void DrawSectionHeader(string sectionTitle, bool sectionBool)
    {
        DrawUILine(Color.black);
        EditorPrefs.SetBool(sectionTitle,
            EditorGUILayout.Foldout(sectionBool, sectionTitle, _sectionHeaderStyle));
        if (sectionBool)
            GUILayout.Space(10);
    }

    void CreateEnvelopeMod(string propertyName, string settingsName)
    {
        SerializedProperty filterList = serializedObject.FindProperty(propertyName);
        var newEnvelope = _settingsObject.AddElement<SynthSettingsObjectEnvelope>(filterList, settingsName);
        newEnvelope.attack = 0.1f;
        newEnvelope.decay = 0.5f;
        newEnvelope.sustain = 0.5f;
        newEnvelope.release = 0.5f;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_settingsObject);
        _settingsObject.RebuildSynth();
    }

    void CreateLFOMod(string propertyName, string settingsName)
    {
        SerializedProperty filterList = serializedObject.FindProperty(propertyName);
        var newEnvelope = _settingsObject.AddElement<SynthSettingsObjectLFO>(filterList, settingsName);
        //newEnvelope.amp = 1;
        newEnvelope.frequency = 10;
        newEnvelope.fadeInDuration = 0.01f;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_settingsObject);
        _settingsObject.RebuildSynth();
    }

    void CreateOscillator(string propertyName, string settingsName)
    {
        SerializedProperty filterList = serializedObject.FindProperty(propertyName);
        var newOSC = _settingsObject.AddElement<SynthSettingsObjectOscillator>(filterList, settingsName);
        newOSC.Init();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_settingsObject);
        _settingsObject.RebuildSynth();
    }

    void CreateFilter(string propertyName, string settingsName)
    {
        SerializedProperty filterList = serializedObject.FindProperty(propertyName);
        var newFilter = _settingsObject.AddElement<SynthSettingsObjectFilter>(filterList, settingsName);
        newFilter.Init();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_settingsObject);
        _settingsObject.RebuildSynth();
    }

    public void RebuildSynth()
    {
        _settingsObject.RebuildSynth();
    }

    private void InitPreset()
    {
        Debug.Log("init preset");
        CreateOscillator("oscillatorSettings", "Oscillator");
        CreateFilter("filterSettings", "Filter");
        _settingsObject.isInit = true;
        EditorUtility.SetDirty(_settingsObject);
    }

    public void DeleteElement<T>(SynthSettingsObjectBase synthSettings, string listName) where T : ScriptableObject
    {
        SerializedProperty filterList = serializedObject.FindProperty(listName);
        synthSettings.RemoveElement<T>(filterList);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_settingsObject);
        _settingsObject.CleanUpPreset();
    }
}