using Anywhen;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    [CustomEditor(typeof(AnywhenRuntime))]
    public class AnywhenInspector : UnityEditor.Editor
    {
        public enum OPTIONS
        {
            RETRO = 0,
            FANTASY = 1,
            SCIFI = 2,
            WHIMSICAL = 3,
        }

        public OPTIONS op;
        private int _tab;

        Texture2D _texture;
        private float _textureAspect;
        private float _textureMaxWidth = 800;
        private float _currentWidth;
        private float _slider1, _slider2, _slider3;

        void OnEnable()
        {
            _texture = Resources.Load<Texture2D>("ANYWHENLOGO");
            _textureAspect = (float)_texture.width / _texture.height;
            _textureMaxWidth = 500;
        }


        public override void OnInspectorGUI()
        {
            _currentWidth = Mathf.Min(_textureMaxWidth, GetViewWidth());

            Rect rt = GUILayoutUtility.GetRect(_currentWidth, _currentWidth, _currentWidth / _textureAspect,
                _currentWidth / _textureAspect);

            GUI.DrawTexture(
                new Rect(GetViewWidth() / 2f - (_currentWidth / 2), 0, _currentWidth,
                    _currentWidth / _textureAspect), _texture);


            //EditorGUILayout.Knob(new Vector2(100, 100), 0, 0, 11, "", Color.black, Color.green, true);
            EditorGUILayout.LabelField("Anywhen is running");

            EditorGUILayout.Separator();
            op = (OPTIONS)EditorGUILayout.EnumPopup("Current musical style:", op);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Variations:");
            _tab = GUILayout.Toolbar(_tab, new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" });


            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("RUNTIME VARIABLES");
            _slider1 = EditorGUILayout.Slider("Intensity", _slider1, 0, 1);
            _slider2 = EditorGUILayout.Slider("Complexity", _slider2, 0, 1);
            _slider3 = EditorGUILayout.Slider("Variation", _slider3, 0, 1);
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();

            GUILayout.Button("SAVE COMPOSITION");
            GUILayout.Button("LOAD COMPOSITION");

            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
        }

        private Rect _rect;

        private float GetViewWidth()
        {
            GUILayout.Label("hack", GUILayout.MaxHeight(0));
            if (Event.current.type == EventType.Repaint)
            {
                // hack to get real view width
                _rect = GUILayoutUtility.GetLastRect();
            }

            return _rect.width;
        }
    }
}