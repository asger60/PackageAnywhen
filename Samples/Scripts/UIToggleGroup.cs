using System;
using UnityEngine;

public class UIToggleGroup : MonoBehaviour
{
   private UIButton[] _childButtons;
   private int _selectedButtonIndex;

   private void Start()
   {
      _childButtons = GetComponentsInChildren<UIButton>();
      foreach (UIButton uiButton in _childButtons)
      {
         uiButton.Init(this);
      }
      SelectButton(_childButtons[0]);
   }

   public void SelectButton(UIButton button)
   {
      foreach (var childButton in _childButtons)
      {
         childButton.SetIsSelected(childButton == button);
      }
   }
}
