using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour
{
    private UIToggleGroup _toggleGroup;
    private Button _button;
    private Image _image;
    public void Init(UIToggleGroup toggleGroup)
    {
        _toggleGroup = toggleGroup;
        TryGetComponent(out _button);
        TryGetComponent(out _image);
        _button.onClick.AddListener((() =>
        {
            _toggleGroup.SelectButton(this);
        }));
    }
    public void SetIsSelected(bool state)
    {
        _image.color = state ? Color.yellow : Color.white;
    }
}
