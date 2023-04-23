using System;
using UnityEngine;

public class UIToggleGroup : MonoBehaviour
{
    private UIButton[] _childButtons;
    private int _selectedButtonIndex;
    public Action<int> OnSelect;

    private void Start()
    {
        _childButtons = GetComponentsInChildren<UIButton>();
        foreach (UIButton uiButton in _childButtons)
        {
            uiButton.Init(this);
        }

        SelectButton(_childButtons[0]);
    }

    public void SelectButton(UIButton button, bool notify = true)
    {
        int selectedIndex = 0;
        for (var i = 0; i < _childButtons.Length; i++)
        {
            var childButton = _childButtons[i];
            childButton.SetIsSelected(childButton == button);
            if (button == childButton)
                selectedIndex = i;
        }

        if (notify)
            OnSelect?.Invoke(selectedIndex);
    }
    public void SelectButton(int buttonIndex, bool notify = true)
    {
        int selectedIndex = buttonIndex ;
        for (var i = 0; i < _childButtons.Length; i++)
        {
            var childButton = _childButtons[i];
            childButton.SetIsSelected(i == buttonIndex);
        }

        if (notify)
            OnSelect?.Invoke(selectedIndex);
    }
}