using Anywhen;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnywhenRuntime))]
public class AnywhenRuntimeInspector : UnityEditor.Editor
{
    public enum OPTIONS
    {
        RETRO = 0,
        FANTASY = 1,
        SCIFI = 2,
        WHIMSICAL = 3,
    }

    public OPTIONS op;

    Texture2D _texture;
    private float _textureAspect;
    private float _textureMaxWidth = 800;
    private float _currentWidth;
    private float _slider1, _slider2, _slider3;
    private AnywhenRuntime _anywhenRuntime;

    void OnEnable()
    {
        _texture = Resources.Load<Texture2D>("ANYWHENLOGO");
        _textureAspect = (float)_texture.width / _texture.height;
        _textureMaxWidth = 500;
        _anywhenRuntime = (AnywhenRuntime)target;
    }


    public override void OnInspectorGUI()
    {
        _currentWidth = Mathf.Min(_textureMaxWidth, GetViewWidth());

        Rect rt = GUILayoutUtility.GetRect(_currentWidth, _currentWidth, _currentWidth / _textureAspect,
            _currentWidth / _textureAspect);

        GUI.DrawTexture(
            new Rect(GetViewWidth() / 2f - (_currentWidth / 2), 0, _currentWidth,
                _currentWidth / _textureAspect), _texture);
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