using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class RaycastTest
{
    static Event e
    {
        get { return Event.current; }
    }

    private static readonly Canvas canvas;

    static RaycastTest()
    {
        canvas = Object.FindObjectOfType<Canvas>();
        SceneView.duringSceneGui += GUIUpdate;
    }

    static void GUIUpdate(SceneView sceneview)
    {
        switch (e.type)
        {
            //case EventType.Repaint:
            //    break;
            case EventType.MouseDown:
                TestCast();
                break;
            //case EventType.MouseUp: 
            //    break;
            //case EventType.MouseDrag:
            //    break;
            //case EventType.Layout:
            //    break;
        }
    }

    static void TestCast()
    {
        Camera sceneCamera = SceneView.lastActiveSceneView.camera;

        Graphic frontGraphic = null;
        // Get mouse screen position
        Vector3 mouseScreenPosition = HandleUtility.GUIPointToScreenPixelCoordinate(e.mousePosition);
        // Get the list of all graphics in the scene
        IList<Graphic> graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
        // Get the front-most graphic on the position you clicked
        for (int i = 0; i < graphics.Count; i++)
        {
            Graphic graphic = graphics[i];

            if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, mouseScreenPosition,
                    sceneCamera, graphic.raycastPadding))
                continue;

            if (sceneCamera != null && sceneCamera.WorldToScreenPoint(graphic.rectTransform.position).z >
                sceneCamera.farClipPlane)
                continue;

            if (graphic.Raycast(mouseScreenPosition, sceneCamera))
            {
                if (frontGraphic == null || frontGraphic.depth < graphic.depth)
                {
                    frontGraphic = graphic;
                }
            }
        }

        if (frontGraphic != null)
        {
            Debug.Log("You are clicking on " + frontGraphic.name);
        }
    }
}