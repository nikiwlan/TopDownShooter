using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Tooltip("Der Canvas, in dem das Crosshair liegt")]
    public RectTransform canvasRect;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // System-Mauszeiger ausblenden
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Mausposition in lokale Canvas-Koordinaten umrechnen
        if (canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            null,                 // Canvas ist Screen Space - Overlay
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint;
    }
}
