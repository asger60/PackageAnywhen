#if UNITY_EDITOR
using Anywhen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Anysong
{
    [CustomEditor(typeof(AnysongPlayer))]
    public class AnysongPlayerInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            var songObject = serializedObject.FindProperty("songObject");
            var songObjectField = new PropertyField(songObject);
            songObjectField.BindProperty(songObject);
            inspector.Add(songObjectField);

            var triggerObject = serializedObject.FindProperty("trigger");


            var triggerObjectField = new PropertyField(triggerObject);
            triggerObjectField.BindProperty(triggerObject);
            inspector.Add(triggerObjectField);


            var transitionTypeObject = serializedObject.FindProperty("triggerTransitionsType");
            var transitionObjectField = new PropertyField(transitionTypeObject);
            transitionObjectField.BindProperty(transitionTypeObject);
            inspector.Add(transitionObjectField);

            var playButton = new Button
            {
                text = "Preview"
            };
            playButton.clicked += () =>
            {
                var anysongPlayer = target as AnysongPlayer;
                anysongPlayer?.ToggleEditorPreview();
            };
            inspector.Add(playButton);

            return inspector;
        }
    }
}
#endif