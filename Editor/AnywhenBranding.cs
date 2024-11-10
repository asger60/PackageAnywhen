using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR

    public static class AnywhenBranding
    {
        public static VisualElement DrawBranding()
        {
            var element = new VisualElement();
            string path = AnywhenMenuUtils.GetAssetPath("Editor/uxml/AnywhenBranding.uxml");
            VisualTreeAsset uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            VisualElement ui = uiAsset.Instantiate();
            element.Add(ui);
            return element;
        }
    }

#endif